/********************************************************************************
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

using Org.Eclipse.TractusX.Portal.Backend.Framework.ErrorHandling;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Models;
using Org.Eclipse.TractusX.Portal.Backend.Offers.Library.Models;
using Org.Eclipse.TractusX.Portal.Backend.Offers.Library.Service;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Repositories;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using Org.Eclipse.TractusX.Portal.Backend.Services.Service.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Org.Eclipse.TractusX.Portal.Backend.Services.Service.BusinessLogic;

/// <summary>
/// Implementation of <see cref="IServiceBusinessLogic"/>.
/// </summary>
public class ServiceBusinessLogic : IServiceBusinessLogic
{
    private readonly IPortalRepositories _portalRepositories;
    private readonly IOfferService _offerService;
    private readonly ServiceSettings _settings;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="portalRepositories">Factory to access the repositories</param>
    /// <param name="settings">Access to the settings</param>
    /// <param name="offerService">Access to the offer service</param>
    public ServiceBusinessLogic(
        IPortalRepositories portalRepositories, 
        IOptions<ServiceSettings> settings, 
        IOfferService offerService)
    {
        _portalRepositories = portalRepositories;
        _offerService = offerService;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public Task<Pagination.Response<ServiceOverviewData>> GetAllActiveServicesAsync(int page, int size)
    {
        var services = _portalRepositories.GetInstance<IOfferRepository>().GetActiveServices();
        return Pagination.CreateResponseAsync(
            page,
            size,
            _settings.ApplicationsMaxPageSize,
            (skip, take) => new Pagination.AsyncSource<ServiceOverviewData>(
                services.CountAsync(),
                services
                    .Skip(skip)
                    .Take(take)
                    .Select(s =>
                        new ServiceOverviewData(
                            s.id,
                            s.name ?? Constants.ErrorString,
                            s.provider,
                            s.thumbnailUrl ?? Constants.ErrorString,
                            s.contactEmail,
                            null,
                            s.price ?? Constants.ErrorString))
                    .AsAsyncEnumerable()));
    }

    /// <inheritdoc />
    public async Task<Guid> CreateServiceOffering(ServiceOfferingData data, string iamUserId)
    {
        var results = await _portalRepositories.GetInstance<IUserRepository>()
            .GetCompanyUserWithIamUserCheckAndCompanyShortName(iamUserId, data.SalesManager)
            .ToListAsync();

        if (!results.Any(x => x.IsIamUser))
            throw new ControllerArgumentException($"IamUser is not assignable to company user {iamUserId}", nameof(iamUserId));

        if (string.IsNullOrWhiteSpace(results.Single(x => x.IsIamUser).CompanyShortName))
            throw new ControllerArgumentException($"No matching company found for user {iamUserId}", nameof(iamUserId));

        if (results.All(x => x.CompanyUserId != data.SalesManager))
            throw new ControllerArgumentException("SalesManager does not exist", nameof(data.SalesManager));

        await CheckLanguageCodesExist(data.Descriptions.Select(x => x.LanguageCode)).ConfigureAwait(false);

        var offerRepository = _portalRepositories.GetInstance<IOfferRepository>();
        var service = offerRepository.CreateOffer(string.Empty, OfferTypeId.SERVICE, service =>
        {
            service.ContactEmail = data.ContactEmail;
            service.Name = data.Title;
            service.SalesManagerId = data.SalesManager;
            service.ThumbnailUrl = data.ThumbnailUrl;
            service.Provider = results.Single(x => x.IsIamUser).CompanyShortName;
            service.OfferStatusId = OfferStatusId.CREATED;
            service.ProviderCompanyId = results.Single(x => x.IsIamUser).CompanyId;
        });
        var licenseId = offerRepository.CreateOfferLicenses(data.Price).Id;
        offerRepository.CreateOfferAssignedLicense(service.Id, licenseId);
        offerRepository.AddOfferDescriptions(data.Descriptions.Select(d =>
            new ValueTuple<Guid, string, string, string>(service.Id, d.LanguageCode, string.Empty, d.Description)));

        await _portalRepositories.SaveAsync();
        return service.Id;
    }

    /// <inheritdoc />
    public async Task<Guid> AddServiceSubscription(Guid serviceId, string iamUserId)
    {
        if (!await _portalRepositories.GetInstance<IOfferRepository>().CheckServiceExistsById(serviceId).ConfigureAwait(false))
        {
            throw new NotFoundException($"Service {serviceId} does not exist");
        }

        var (companyId, companyUserId) = await _portalRepositories.GetInstance<IUserRepository>().GetOwnCompanAndCompanyUseryId(iamUserId).ConfigureAwait(false);
        if (companyId == Guid.Empty)
        {
            throw new ControllerArgumentException($"User {iamUserId} has no company assigned", nameof(iamUserId));
        }
        
        if (companyUserId == Guid.Empty)
        {
            throw new ControllerArgumentException($"User {iamUserId} has no company user assigned", nameof(iamUserId));
        }

        var offerSubscription = _portalRepositories.GetInstance<IOfferSubscriptionsRepository>().CreateOfferSubscription(serviceId, companyId, OfferSubscriptionStatusId.PENDING, companyUserId, companyUserId);
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
        return offerSubscription.Id;
    }

    /// <inheritdoc />
    public async Task<OfferDetailData> GetServiceDetailsAsync(Guid serviceId, string lang, string iamUserId)
    {        
        var serviceDetailData = await _portalRepositories.GetInstance<IOfferRepository>().GetOfferDetailByIdUntrackedAsync(serviceId, lang, iamUserId, OfferTypeId.SERVICE).ConfigureAwait(false);
        if (serviceDetailData == default)
        {
            throw new NotFoundException($"Service {serviceId} does not exist");
        }

        return serviceDetailData;
    }

    /// <inheritdoc />
    public async Task<SubscriptionDetailData> GetSubscriptionDetailAsync(Guid subscriptionId, string iamUserId)
    {
        var subscriptionDetailData = await _portalRepositories.GetInstance<IOfferSubscriptionsRepository>()
            .GetSubscriptionDetailDataForOwnUserAsync(subscriptionId, iamUserId, OfferTypeId.SERVICE).ConfigureAwait(false);
        if (subscriptionDetailData is null)
        {
            throw new NotFoundException($"Subscription {subscriptionId} does not exist");
        }

        return subscriptionDetailData;
    }

    /// <inheritdoc />
    public Task<Guid> CreateServiceAgreementConsentAsync(Guid subscriptionId,
        ServiceAgreementConsentData serviceAgreementConsentData, string iamUserId) =>
        _offerService.CreateOfferSubscriptionAgreementConsentAsync(subscriptionId, serviceAgreementConsentData.AgreementId,
            serviceAgreementConsentData.ConsentStatusId, iamUserId, OfferTypeId.SERVICE);

    /// <inheritdoc />
    public IAsyncEnumerable<AgreementData> GetServiceAgreement(Guid serviceId) => 
        _offerService.GetOfferAgreementsAsync(serviceId, OfferTypeId.SERVICE);

    /// <inheritdoc />
    public Task<ConsentDetailData> GetServiceConsentDetailDataAsync(Guid serviceConsentId) =>
        _offerService.GetConsentDetailDataAsync(serviceConsentId, OfferTypeId.SERVICE);

    /// <inheritdoc />
    public Task CreateOrUpdateServiceAgreementConsentAsync(Guid subscriptionId,
        IEnumerable<ServiceAgreementConsentData> serviceAgreementConsentDatas,
        string iamUserId) =>
        _offerService.CreateOrUpdateOfferSubscriptionAgreementConsentAsync(subscriptionId, serviceAgreementConsentDatas, iamUserId, OfferTypeId.SERVICE);

    /// <inheritdoc />
    public Task<OfferAutoSetupResponseData> AutoSetupService(OfferAutoSetupData data, string iamUserId) =>
        _offerService.AutoSetupServiceAsync(data, _settings.ServiceAccountRoles, _settings.CompanyAdminRoles, iamUserId, OfferTypeId.APP);

    private async Task CheckLanguageCodesExist(IEnumerable<string> languageCodes)
    {
        if (languageCodes.Any())
        {
            var foundLanguageCodes = await _portalRepositories.GetInstance<ILanguageRepository>()
                .GetLanguageCodesUntrackedAsync(languageCodes)
                .ToListAsync()
                .ConfigureAwait(false);
            var notFoundLanguageCodes = languageCodes.Except(foundLanguageCodes).ToList();
            if (notFoundLanguageCodes.Any())
            {
                throw new ControllerArgumentException(
                    $"Language code(s) {string.Join(",", notFoundLanguageCodes)} do(es) not exist",
                    nameof(languageCodes));
            }
        }
    }
}