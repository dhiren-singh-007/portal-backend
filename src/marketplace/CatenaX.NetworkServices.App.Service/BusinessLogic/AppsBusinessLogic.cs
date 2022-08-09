﻿/********************************************************************************
 * Copyright (c) 2021,2022 BMW Group AG
 * Copyright (c) 2021,2022 Contributors to the CatenaX (ng) GitHub Organisation.
 *
 * See the NOTICE file(s) distributed with this work for additional
 * information regarding copyright ownership.
 *
 * This program and the accompanying materials are made available under the
 * terms of the Apache License, Version 2.0 which is available at
 * https://www.apache.org/licenses/LICENSE-2.0.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 * SPDX-License-Identifier: Apache-2.0
 ********************************************************************************/

using System.Text.Json;
using CatenaX.NetworkServices.App.Service.InputModels;
using CatenaX.NetworkServices.Framework.ErrorHandling;
using CatenaX.NetworkServices.PortalBackend.DBAccess.Repositories;
using CatenaX.NetworkServices.Mailing.SendMail;
using CatenaX.NetworkServices.PortalBackend.DBAccess;
using CatenaX.NetworkServices.PortalBackend.DBAccess.Models;
using CatenaX.NetworkServices.PortalBackend.PortalEntities.Entities;
using CatenaX.NetworkServices.PortalBackend.PortalEntities.Enums;
using Microsoft.EntityFrameworkCore;

namespace CatenaX.NetworkServices.App.Service.BusinessLogic;

/// <summary>
/// Implementation of <see cref="IAppsBusinessLogic"/>.
/// </summary>
public class AppsBusinessLogic : IAppsBusinessLogic
{
    private readonly IPortalRepositories _portalRepositories;
    private readonly IMailingService _mailingService;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="portalRepositories">Factory to access the repositories</param>
    /// <param name="mailingService">Mail service.</param>
    public AppsBusinessLogic(IPortalRepositories portalRepositories, IMailingService mailingService)
    {
        _portalRepositories = portalRepositories;
        _mailingService = mailingService;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<AppData> GetAllActiveAppsAsync(string? languageShortName = null) =>
        _portalRepositories.GetInstance<IAppRepository>().GetAllActiveAppsAsync(languageShortName);

    /// <inheritdoc/>
    public IAsyncEnumerable<BusinessAppData> GetAllUserUserBusinessAppsAsync(string userId) =>
        _portalRepositories.GetInstance<IUserRepository>().GetAllBusinessAppDataForUserIdAsync(userId);

    /// <inheritdoc/>
    public Task<AppDetailsData> GetAppDetailsByIdAsync(Guid appId, string iamUserId, string? languageShortName = null) =>
        _portalRepositories.GetInstance<IAppRepository>()
            .GetAppDetailsByIdAsync(appId, iamUserId, languageShortName);

    /// <inheritdoc/>
    public IAsyncEnumerable<Guid> GetAllFavouriteAppsForUserAsync(string userId) =>
        _portalRepositories
            .GetInstance<IUserRepository>()
            .GetAllFavouriteAppsForUserUntrackedAsync(userId);

    /// <inheritdoc/>
    public async Task RemoveFavouriteAppForUserAsync(Guid appId, string userId)
    {
        try
        {
            var companyUserId = await _portalRepositories.GetInstance<IUserRepository>().GetCompanyUserIdForIamUserUntrackedAsync(userId).ConfigureAwait(false);
            _portalRepositories.Remove(new CompanyUserAssignedAppFavourite(appId, companyUserId));
            await _portalRepositories.SaveAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ArgumentException($"Parameters are invalid or favourite does not exist.");
        }
    }

    /// <inheritdoc/>
    public async Task AddFavouriteAppForUserAsync(Guid appId, string userId)
    {
        try
        {
            var companyUserId = await _portalRepositories.GetInstance<IUserRepository>().GetCompanyUserIdForIamUserUntrackedAsync(userId).ConfigureAwait(false);
            _portalRepositories.GetInstance<IAppRepository>().CreateAppFavourite(appId, companyUserId);
            await _portalRepositories.SaveAsync().ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new ArgumentException($"Parameters are invalid or app is already favourited.");
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<AppWithSubscriptionStatus> GetCompanySubscribedAppSubscriptionStatusesForUserAsync(string iamUserId) =>
        _portalRepositories.GetInstance<ICompanyAssignedAppsRepository>()
            .GetOwnCompanySubscribedAppSubscriptionStatusesUntrackedAsync(iamUserId);

    /// <inheritdoc/>
    public IAsyncEnumerable<AppCompanySubscriptionStatusData> GetCompanyProvidedAppSubscriptionStatusesForUserAsync(string iamUserId) =>
        _portalRepositories.GetInstance<ICompanyAssignedAppsRepository>()
            .GetOwnCompanyProvidedAppSubscriptionStatusesUntrackedAsync(iamUserId);

    /// <inheritdoc/>
    public async Task AddOwnCompanyAppSubscriptionAsync(Guid appId, string iamUserId)
    {
        var appDetails = await _portalRepositories.GetInstance<IAppRepository>().GetAppProviderDetailsAsync(appId).ConfigureAwait(false);
        if (appDetails == null)
        {
            throw new NotFoundException($"App {appId} does not exist");
        }
        
        var (requesterId, requesterEmail) = await _portalRepositories.GetInstance<IUserRepository>()
            .GetCompanyUserIdAndEmailForIamUserUntrackedAsync(iamUserId).ConfigureAwait(false);
        var companyName = await GetCompanyAppSubscriptionData(appId, iamUserId, requesterId);

        if(appDetails.AppName is null || appDetails.ProviderContactEmail is null)
        {
            var nullProperties = new List<string>();
            if (appDetails.AppName is null)
            {
                nullProperties.Add($"{nameof(App)}.{nameof(appDetails.AppName)}");
            }
            if(appDetails.ProviderContactEmail is null)
            {
                nullProperties.Add($"{nameof(App)}.{nameof(appDetails.ProviderContactEmail)}");
            }
            throw new UnexpectedConditionException($"The following fields of app '{appId}' have not been configured properly: {string.Join(", ", nullProperties)}");
        }

        var notificationContent = new
        {
            appDetails.AppName,
            RequestorCompanyName = companyName,
            UserEmail = requesterEmail,
        };
        _portalRepositories.GetInstance<INotificationRepository>().Create(appDetails.SalesManagerId, NotificationTypeId.APP_SUBSCRIPTION_REQUEST, false,
            notification =>
            {
                notification.CreatorUserId = requesterId;
                notification.Content = JsonSerializer.Serialize(notificationContent);
            });
        await _portalRepositories.SaveAsync().ConfigureAwait(false);

        var mailParams = new Dictionary<string, string>
            {
                { "appProviderName", appDetails.ProviderName},
                { "appName", appDetails.AppName }
            };
        await _mailingService.SendMails(appDetails.ProviderContactEmail, mailParams, new List<string> { "subscription-request" }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ActivateOwnCompanyProvidedAppSubscriptionAsync(Guid appId, Guid subscribingCompanyId, string iamUserId)
    {
        var assignedAppData = await _portalRepositories.GetInstance<ICompanyAssignedAppsRepository>().GetCompanyAssignedAppDataForProvidingCompanyUserAsync(appId, subscribingCompanyId, iamUserId).ConfigureAwait(false);
        if(assignedAppData == default)
        {
            throw new NotFoundException($"App {appId} does not exist.");
        }

        var (subscription, isMemberOfCompanyProvidingApp, appName, companyUserId) = assignedAppData;
        if(!isMemberOfCompanyProvidingApp)
        {
            throw new ArgumentException("Missing permission: The user's company does not provide the requested app so they cannot activate it.");
        }

        if (subscription is not { AppSubscriptionStatusId: AppSubscriptionStatusId.PENDING })
        {
            throw new ArgumentException("No pending subscription for provided parameters existing.");
        }
        subscription.AppSubscriptionStatusId = AppSubscriptionStatusId.ACTIVE;

        _portalRepositories.GetInstance<INotificationRepository>().Create(subscription.RequesterId,
            NotificationTypeId.APP_SUBSCRIPTION_ACTIVATION, false,
            notification =>
            {
                notification.CreatorUserId = companyUserId;
                notification.Content = JsonSerializer.Serialize(new
                {
                    AppName = appName
                });
            });
        
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UnsubscribeOwnCompanyAppSubscriptionAsync(Guid appId, string iamUserId)
    {
        var assignedAppData = await _portalRepositories.GetInstance<ICompanyAssignedAppsRepository>().GetCompanyAssignedAppDataForCompanyUserAsync(appId, iamUserId).ConfigureAwait(false);

        if(assignedAppData == default)
        {
            throw new NotFoundException($"App {appId} does not exist.");
        }

        var (subscription, _) = assignedAppData;

        if (subscription == null)
        {
            throw new ArgumentException($"There is no active subscription for user '{iamUserId}' and app '{appId}'");
        }
        subscription.AppSubscriptionStatusId = AppSubscriptionStatusId.INACTIVE;
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateAppAsync(AppInputModel appInputModel)
    {
        // Add app to db
        var appRepository = _portalRepositories.GetInstance<IAppRepository>();

        var appId = appRepository.CreateApp(appInputModel.Provider, app =>
        {
            app.Name = appInputModel.Title;
            app.MarketingUrl = appInputModel.ProviderUri;
            app.AppUrl = appInputModel.AppUri;
            app.ThumbnailUrl = appInputModel.LeadPictureUri;
            app.ContactEmail = appInputModel.ContactEmail;
            app.ContactNumber = appInputModel.ContactNumber;
            app.ProviderCompanyId = appInputModel.ProviderCompanyId;
            app.AppStatusId = AppStatusId.CREATED;
            app.SalesManagerId = appInputModel.SalesManagerId;
        }).Id;

        var licenseId = appRepository.CreateAppLicenses(appInputModel.Price).Id;
        appRepository.CreateAppAssignedLicense(appId, licenseId);
        appRepository.AddAppAssignedUseCases(appInputModel.UseCaseIds.Select(uc =>
            new ValueTuple<Guid, Guid>(appId, uc)));
        appRepository.AddAppDescriptions(appInputModel.Descriptions.Select(d =>
            new ValueTuple<Guid, string, string, string>(appId, d.LanguageCode, d.LongDescription, d.ShortDescription)));
        appRepository.AddAppLanguages(appInputModel.SupportedLanguageCodes.Select(c =>
            new ValueTuple<Guid, string>(appId, c)));

        await _portalRepositories.SaveAsync().ConfigureAwait(false);

        return appId;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<AllAppData> GetCompanyProvidedAppsDataForUserAsync(string userId)=>
        _portalRepositories.GetInstance<IAppRepository>().GetProvidedAppsData(userId);
    
    /// <inheritdoc/>
    public  Task<Guid> AddAppAsync(AppRequestModel appRequestModel)
    {
        if(appRequestModel.ProviderCompanyId == Guid.Empty)
        {
            throw new ControllerArgumentException($"Company Id must be specified", nameof(appRequestModel.ProviderCompanyId)); 
        }
        if (!appRequestModel.SupportedLanguageCodes.Any())
        {
            throw new ControllerArgumentException($"Language Codes must not be empty", nameof(appRequestModel.SupportedLanguageCodes)); 
        }
        if (!appRequestModel.UseCaseIds.Any())
        {
            throw new ControllerArgumentException($"Use Cases must not be empty", nameof(appRequestModel.UseCaseIds)); 
        }
        return CreateAppAsync(appRequestModel);
    }
    
    private async Task<Guid> CreateAppAsync(AppRequestModel appRequestModel)
    {   
        // Add app to db
        var appRepository = _portalRepositories.GetInstance<IAppRepository>();
        var appId = appRepository.CreateApp(appRequestModel.Provider, app =>
        {
            app.Name = appRequestModel.Title;
            app.ThumbnailUrl = appRequestModel.LeadPictureUri;
            app.ProviderCompanyId = appRequestModel.ProviderCompanyId;
            app.AppStatusId = AppStatusId.CREATED;
        }).Id;
        appRepository.AddAppDescriptions(appRequestModel.Descriptions.Select(d =>
              (appId, d.LanguageCode, d.LongDescription, d.ShortDescription)));
        appRepository.AddAppLanguages(appRequestModel.SupportedLanguageCodes.Select(c =>
              (appId, c)));
        appRepository.AddAppAssignedUseCases(appRequestModel.UseCaseIds.Select(uc =>
              (appId, uc)));
        var licenseId = appRepository.CreateAppLicenses(appRequestModel.Price).Id;
        appRepository.CreateAppAssignedLicense(appId, licenseId);
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
        return appId;
    }
    
    private async Task<string> GetCompanyAppSubscriptionData(Guid appId, string iamUserId, Guid requesterId)
    {
        var companyAssignedAppRepository = _portalRepositories.GetInstance<ICompanyAssignedAppsRepository>();
        var companyAppSubscriptionData = await companyAssignedAppRepository
            .GetCompanyIdWithAssignedAppForCompanyUserAsync(appId, iamUserId).ConfigureAwait(false);
        if (companyAppSubscriptionData == default)
        {
            throw new ControllerArgumentException($"user {iamUserId} is not assigned with a company");
        }

        var (companyId, companyAssignedApp, companyName) = companyAppSubscriptionData;
        if (companyAssignedApp == null)
        {
            companyAssignedAppRepository.CreateCompanyAssignedApp(appId, companyId, AppSubscriptionStatusId.PENDING,
                requesterId);
        }
        else
        {
            if (companyAssignedApp.AppSubscriptionStatusId is AppSubscriptionStatusId.ACTIVE
                or AppSubscriptionStatusId.PENDING)
            {
                throw new ArgumentException($"company {companyId} is already subscribed to {appId}");
            }

            companyAssignedApp.AppSubscriptionStatusId = AppSubscriptionStatusId.PENDING;
        }

        return companyName;
    }
}
