/********************************************************************************
 * Copyright (c) 2021, 2023 BMW Group AG
 * Copyright (c) 2021, 2023 Contributors to the Eclipse Foundation
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
using Org.Eclipse.TractusX.Portal.Backend.Framework.IO;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Repositories;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using Org.Eclipse.TractusX.Portal.Backend.Processes.OfferSubscription.Library;
using Org.Eclipse.TractusX.Portal.Backend.Processes.OfferSubscription.Library.Extensions;

namespace Org.Eclipse.TractusX.Portal.Backend.Administration.Service.BusinessLogic;

public class SubscriptionConfigurationBusinessLogic : ISubscriptionConfigurationBusinessLogic
{
    private readonly IOfferSubscriptionProcessService _offerSubscriptionProcessService;
    private readonly IPortalRepositories _portalRepositories;

    public SubscriptionConfigurationBusinessLogic(IOfferSubscriptionProcessService offerSubscriptionProcessService, IPortalRepositories portalRepositories)
    {
        _offerSubscriptionProcessService = offerSubscriptionProcessService;
        _portalRepositories = portalRepositories;
    }

    /// <inheritdoc />
    public async Task<ProviderDetailReturnData> GetProviderCompanyDetailsAsync(Guid companyId)
    {
        var result = await _portalRepositories.GetInstance<ICompanyRepository>()
            .GetProviderCompanyDetailAsync(CompanyRoleId.SERVICE_PROVIDER, companyId)
            .ConfigureAwait(false);
        if (result == default)
        {
            throw new ConflictException($"Company {companyId} not found");
        }
        if (!result.IsProviderCompany)
        {
            throw new ForbiddenException($"Company {companyId} is not a service-provider");
        }

        return result.ProviderDetailReturnData;
    }

    /// <inheritdoc />
    public Task SetProviderCompanyDetailsAsync(ProviderDetailData data, (Guid UserId, Guid CompanyId) identity)
    {
        data.Url.EnsureValidHttpsUrl(() => nameof(data.Url));
        data.CallbackUrl?.EnsureValidHttpsUrl(() => nameof(data.CallbackUrl));

        if (data.Url.Length > 100)
        {
            throw new ControllerArgumentException(
                "the maximum allowed length is 100 characters", nameof(data.Url));
        }

        return SetOfferProviderCompanyDetailsInternalAsync(data, identity);
    }

    private async Task SetOfferProviderCompanyDetailsInternalAsync(ProviderDetailData data, (Guid UserId, Guid CompanyId) identity)
    {
        var companyRepository = _portalRepositories.GetInstance<ICompanyRepository>();
        var providerDetailData = await companyRepository
            .GetProviderCompanyDetailsExistsForUser(identity.CompanyId)
            .ConfigureAwait(false);
        if (providerDetailData == default)
        {
            var result = await companyRepository
                .IsValidCompanyRoleOwner(identity.CompanyId, new[] { CompanyRoleId.APP_PROVIDER, CompanyRoleId.SERVICE_PROVIDER })
                .ConfigureAwait(false);
            if (!result.IsValidCompanyId)
            {
                throw new ConflictException($"Company {identity.CompanyId} not found");
            }
            if (!result.IsCompanyRoleOwner)
            {
                throw new ForbiddenException($"Company {identity.CompanyId} is not an app- or service-provider");
            }
            companyRepository.CreateProviderCompanyDetail(identity.CompanyId, data.Url, providerDetails =>
            {
                if (data.CallbackUrl != null)
                {
                    providerDetails.AutoSetupCallbackUrl = data.CallbackUrl;
                }
                providerDetails.DateLastChanged = DateTimeOffset.UtcNow;
                providerDetails.LastEditorId = identity.UserId;
            });
        }
        else
        {
            companyRepository.AttachAndModifyProviderCompanyDetails(
                providerDetailData.ProviderCompanyDetailId,
                details => { details.AutoSetupUrl = providerDetailData.Url; },
                details =>
                {
                    details.AutoSetupUrl = data.Url;
                    details.DateLastChanged = DateTimeOffset.UtcNow;
                    details.LastEditorId = identity.UserId;
                });
        }
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RetriggerProvider(Guid offerSubscriptionId) =>
        this.TriggerProcessStep(offerSubscriptionId, ProcessStepTypeId.RETRIGGER_PROVIDER, true);

    /// <inheritdoc />
    public Task RetriggerCreateClient(Guid offerSubscriptionId) =>
        this.TriggerProcessStep(offerSubscriptionId, ProcessStepTypeId.RETRIGGER_OFFERSUBSCRIPTION_CLIENT_CREATION, true);

    /// <inheritdoc />
    public Task RetriggerCreateTechnicalUser(Guid offerSubscriptionId) =>
        this.TriggerProcessStep(offerSubscriptionId, ProcessStepTypeId.RETRIGGER_OFFERSUBSCRIPTION_TECHNICALUSER_CREATION, true);

    /// <inheritdoc />
    public Task RetriggerProviderCallback(Guid offerSubscriptionId) =>
        this.TriggerProcessStep(offerSubscriptionId, ProcessStepTypeId.RETRIGGER_PROVIDER_CALLBACK, false);

    /// <inheritdoc />
    private async Task TriggerProcessStep(Guid offerSubscriptionId, ProcessStepTypeId stepToTrigger, bool mustBePending)
    {
        var nextStep = stepToTrigger.GetStepToRetrigger();
        var context = await _offerSubscriptionProcessService.VerifySubscriptionAndProcessSteps(offerSubscriptionId, stepToTrigger, null, mustBePending)
            .ConfigureAwait(false);

        _offerSubscriptionProcessService.FinalizeProcessSteps(context, Enumerable.Repeat(nextStep, 1));
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ProcessStepData> GetProcessStepsForSubscription(Guid offerSubscriptionId) =>
        _portalRepositories.GetInstance<IOfferSubscriptionsRepository>().GetProcessStepsForSubscription(offerSubscriptionId);
}
