/********************************************************************************
 * Copyright (c) 2022 BMW Group AG
 * Copyright (c) 2022 Contributors to the Eclipse Foundation
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

using Microsoft.EntityFrameworkCore;
using Org.Eclipse.TractusX.Portal.Backend.Framework.DBAccess;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Entities;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;

namespace Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Repositories;

/// <inheritdoc />
public class ConsentRepository : IConsentRepository
{
    private readonly PortalDbContext _portalDbContext;

    /// <summary>
    /// Creates an instance of <see cref="ConsentRepository"/>
    /// </summary>
    /// <param name="portalDbContext">The database</param>
    public ConsentRepository(PortalDbContext portalDbContext)
    {
        _portalDbContext = portalDbContext;
    }

    /// <inheritdoc/>
    Consent IConsentRepository.CreateConsent(Guid agreementId, Guid companyId, Guid companyUserId, ConsentStatusId consentStatusId, Action<Consent>? setupOptionalFields)
    {
        var consent = new Consent(Guid.NewGuid(), agreementId, companyId, companyUserId, consentStatusId, DateTimeOffset.UtcNow);
        setupOptionalFields?.Invoke(consent);
        return _portalDbContext.Consents.Add(consent).Entity;
    }

    public void CreateConsents(IEnumerable<(Guid AgreementId, Guid CompanyId, Guid CompanyUserId, ConsentStatusId ConsentStatusId)> consentStatusIds)
    {
        var now = DateTimeOffset.UtcNow;
        _portalDbContext.Consents.AddRange(
            consentStatusIds.Select(x => new Consent(Guid.NewGuid(), x.AgreementId, x.CompanyId, x.CompanyUserId, x.ConsentStatusId, now)));
    }

    /// <inheritdoc />
    public void RemoveConsents(IEnumerable<Consent> consents) =>
        _portalDbContext.Consents.RemoveRange(consents);

    ///<inheritdoc/>
    public ConsentAssignedOffer CreateConsentAssignedOffer(Guid consentId, Guid offerId) =>
        _portalDbContext.ConsentAssignedOffers.Add(new ConsentAssignedOffer(consentId, offerId)).Entity;

    /// <inheritdoc />
    public Task<ConsentDetailData?> GetConsentDetailData(Guid consentId, OfferTypeId offerTypeId) =>
        _portalDbContext.Consents
            .Where(consent =>
                consent.Id == consentId &&
                consent.Agreement!.AgreementAssignedOffers.Any(aao => aao.Offer!.OfferTypeId == offerTypeId))
            .Select(x => new ConsentDetailData(
                x.Id,
                x.Company!.Name,
                x.CompanyUserId,
                x.ConsentStatusId,
                x.Agreement!.Name
            ))
            .SingleOrDefaultAsync();

    /// <inheritdoc />
    public void AttachAndModifiesConsents(IEnumerable<Guid> consentIds, Action<Consent> setOptionalParameter)
    {
        foreach (var consentId in consentIds)
        {
            var consent = _portalDbContext.Consents.Attach(new Consent(consentId, Guid.Empty, Guid.Empty, Guid.Empty, default, default)).Entity;
            setOptionalParameter.Invoke(consent);
        }
    }

    public IEnumerable<Consent> AddAttachAndModifyOfferConsents(IEnumerable<AppAgreementConsentStatus> initialItems, IEnumerable<AgreementConsentStatus> modifyItems, Guid offerId, Guid companyId, Guid companyUserId, DateTimeOffset utcNow) =>
        _portalDbContext.AddAttachRange(
            initialItems,
            modifyItems,
            initial => initial.AgreementId,
            modify => modify.AgreementId,
            initial => new Consent(initial.ConsentId, initial.AgreementId, Guid.Empty, Guid.Empty, initial.ConsentStatusId, default),
            modify =>
            {
                var consent = new Consent(Guid.NewGuid(), modify.AgreementId, companyId, companyUserId, modify.ConsentStatusId, utcNow);
                CreateConsentAssignedOffer(consent.Id, offerId);
                return consent;
            },
            (initial, modify) => initial.ConsentStatusId == modify.ConsentStatusId,
            (consent, modify) =>
            {
                consent.ConsentStatusId = modify.ConsentStatusId;
            });

    public IEnumerable<Consent> AddAttachAndModifyConsents(IEnumerable<ConsentStatusDetails> initialItems, IEnumerable<(Guid AgreementId, ConsentStatusId ConsentStatusId)> modifyItems, Guid companyId, Guid companyUserId, DateTimeOffset utcNow) =>
        _portalDbContext.AddAttachRange(
            initialItems,
            modifyItems,
            initial => initial.AgreementId,
            modify => modify.AgreementId,
            initial => new Consent(initial.ConsentId, initial.AgreementId, Guid.Empty, Guid.Empty, initial.ConsentStatusId, default),
            modify => new Consent(Guid.NewGuid(), modify.AgreementId, companyId, companyUserId, modify.ConsentStatusId, utcNow),
            (initial, modify) => initial.ConsentStatusId == modify.ConsentStatusId,
            (consent, modify) =>
            {
                consent.ConsentStatusId = modify.ConsentStatusId;
            });
}
