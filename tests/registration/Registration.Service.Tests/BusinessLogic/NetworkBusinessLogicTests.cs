/********************************************************************************
 * Copyright (c) 2023 Contributors to the Eclipse Foundation
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

using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using FakeItEasy;
using FluentAssertions;
using Org.Eclipse.TractusX.Portal.Backend.Framework.ErrorHandling;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Identity;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Models.Configuration;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Processes.Library.Concrete.Entities;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Processes.Library.DBAccess;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Processes.Library.Entities;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Processes.Library.Enums;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Processes.Library.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Repositories;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Entities;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using Org.Eclipse.TractusX.Portal.Backend.Processes.ApplicationChecklist.Library;
using Org.Eclipse.TractusX.Portal.Backend.Provisioning.Library.Service;
using Org.Eclipse.TractusX.Portal.Backend.Registration.Service.BusinessLogic;
using Org.Eclipse.TractusX.Portal.Backend.Registration.Service.ErrorHandling;
using Org.Eclipse.TractusX.Portal.Backend.Registration.Service.Model;
using Org.Eclipse.TractusX.Portal.Backend.Tests.Shared.Extensions;
using Xunit;

namespace Org.Eclipse.TractusX.Portal.Backend.Registration.Service.Tests.BusinessLogic;

public class NetworkBusinessLogicTests
{
    private const string Bpnl = "BPNL00000001TEST";
    private static readonly string ExistingExternalId = Guid.NewGuid().ToString();
    private static readonly Guid UserRoleId = Guid.NewGuid();
    private static readonly Guid MultiIdpCompanyId = Guid.NewGuid();
    private static readonly Guid NoIdpCompanyId = Guid.NewGuid();
    private static readonly Guid IdpId = Guid.NewGuid();
    private static readonly Guid NoAliasIdpCompanyId = Guid.NewGuid();
    private static readonly Guid IdentityCompanyId = Guid.NewGuid();

    private readonly IFixture _fixture;

    private readonly IIdentityData _identity;
    private readonly IUserProvisioningService _userProvisioningService;
    private readonly IApplicationChecklistCreationService _checklistService;

    private readonly IPortalRepositories _portalRepositories;
    private readonly ICompanyRepository _companyRepository;
    private readonly IPortalProcessStepRepository _processStepRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly INetworkRepository _networkRepository;
    private readonly IIdentityProviderRepository _identityProviderRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly NetworkBusinessLogic _sut;
    private readonly IConsentRepository _consentRepository;
    private readonly IInvitationRepository _invitationRepository;

    public NetworkBusinessLogicTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization { ConfigureMembers = true });
        _fixture.ConfigureFixture();

        _userProvisioningService = A.Fake<IUserProvisioningService>();
        _portalRepositories = A.Fake<IPortalRepositories>();
        _checklistService = A.Fake<IApplicationChecklistCreationService>();

        _companyRepository = A.Fake<ICompanyRepository>();
        _processStepRepository = A.Fake<IPortalProcessStepRepository>();
        _applicationRepository = A.Fake<IApplicationRepository>();
        _networkRepository = A.Fake<INetworkRepository>();
        _identityProviderRepository = A.Fake<IIdentityProviderRepository>();
        _countryRepository = A.Fake<ICountryRepository>();
        _consentRepository = A.Fake<IConsentRepository>();
        _invitationRepository = A.Fake<IInvitationRepository>();

        var identityService = A.Fake<IIdentityService>();
        _identity = A.Fake<IIdentityData>();
        A.CallTo(() => _identity.IdentityId).Returns(Guid.NewGuid());
        A.CallTo(() => _identity.IdentityTypeId).Returns(IdentityTypeId.COMPANY_USER);
        A.CallTo(() => _identity.CompanyId).Returns(IdentityCompanyId);
        A.CallTo(() => identityService.IdentityData).Returns(_identity);

        A.CallTo(() => _portalRepositories.GetInstance<ICompanyRepository>()).Returns(_companyRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IConsentRepository>()).Returns(_consentRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IPortalProcessStepRepository>()).Returns(_processStepRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IProcessStepRepository<ProcessTypeId, ProcessStepTypeId>>()).Returns(_processStepRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IApplicationRepository>()).Returns(_applicationRepository);
        A.CallTo(() => _portalRepositories.GetInstance<INetworkRepository>()).Returns(_networkRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IIdentityProviderRepository>()).Returns(_identityProviderRepository);
        A.CallTo(() => _portalRepositories.GetInstance<ICountryRepository>()).Returns(_countryRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IInvitationRepository>()).Returns(_invitationRepository);

        _sut = new NetworkBusinessLogic(_portalRepositories, identityService, _checklistService);

        SetupRepos();
    }

    #region Submit

    [Fact]
    public async Task Submit_WithNotExistingSubmitData_ThrowsNotFoundException()
    {
        // Arrange
        var data = _fixture.Create<PartnerSubmitData>();
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns<(bool, IEnumerable<(Guid, CompanyApplicationStatusId, string?)>, IEnumerable<(CompanyRoleId, IEnumerable<Guid>)>, Guid?)>(default);

        // Act
        async Task Act() => await _sut.Submit(data);

        // Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_COMPANY_NOT_FOUND.ToString());
    }

    [Fact]
    public async Task Submit_WithoutCompanyApplications_ThrowsConflictException()
    {
        // Arrange
        var data = _fixture.Create<PartnerSubmitData>();
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns((true, Enumerable.Empty<(Guid, CompanyApplicationStatusId, string?)>(), Enumerable.Empty<(CompanyRoleId, IEnumerable<Guid>)>(), null));

        // Act
        async Task Act() => await _sut.Submit(data);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_CONFLICT_ONLY_ONE_APPLICATION_PER_COMPANY.ToString());
    }

    [Fact]
    public async Task Submit_WithMultipleCompanyApplications_ThrowsConflictException()
    {
        // Arrange
        var data = _fixture.Create<PartnerSubmitData>();
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns((true, _fixture.CreateMany<(Guid, CompanyApplicationStatusId, string?)>(2), Enumerable.Empty<(CompanyRoleId, IEnumerable<Guid>)>(), null));

        // Act
        async Task Act() => await _sut.Submit(data);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_CONFLICT_ONLY_ONE_APPLICATION_PER_COMPANY.ToString());
    }

    [Fact]
    public async Task Submit_WithWrongApplicationStatus_ThrowsConflictException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var data = _fixture.Create<PartnerSubmitData>();
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns((true, Enumerable.Repeat<(Guid, CompanyApplicationStatusId, string?)>((applicationId, CompanyApplicationStatusId.VERIFY, null), 1), Enumerable.Empty<(CompanyRoleId, IEnumerable<Guid>)>(), Guid.NewGuid()));

        // Act
        async Task Act() => await _sut.Submit(data);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_CONFLICT_APP_NOT_CREATED_STATE.ToString());
    }

    [Fact]
    public async Task Submit_WithOneMissingAgreement_ThrowsConflictException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();
        var notExistingAgreementId = Guid.NewGuid();
        var data = new PartnerSubmitData(
            new[] { CompanyRoleId.APP_PROVIDER },
            new[] { new AgreementConsentData(agreementId, ConsentStatusId.ACTIVE) });
        var companyRoleIds = new (CompanyRoleId, IEnumerable<Guid>)[]
        {
            (CompanyRoleId.APP_PROVIDER, new [] {agreementId, notExistingAgreementId})
        };
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns((true, Enumerable.Repeat<(Guid, CompanyApplicationStatusId, string?)>((applicationId, CompanyApplicationStatusId.CREATED, null), 1), companyRoleIds, Guid.NewGuid()));

        // Act
        async Task Act() => await _sut.Submit(data);

        // Assert
        var ex = await Assert.ThrowsAsync<ControllerArgumentException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_ARG_ALL_AGREEMNTS_COMPANY_SHOULD_AGREED.ToString());
    }

    [Fact]
    public async Task Submit_WithOneInactiveAgreement_ThrowsConflictException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();
        var inactiveAgreementId = Guid.NewGuid();
        var data = new PartnerSubmitData(
            new[] { CompanyRoleId.APP_PROVIDER },
            new[]
            {
                new AgreementConsentData(agreementId, ConsentStatusId.ACTIVE),
                new AgreementConsentData(inactiveAgreementId, ConsentStatusId.INACTIVE),
            });
        var companyRoleIds = new (CompanyRoleId, IEnumerable<Guid>)[]
        {
            (CompanyRoleId.APP_PROVIDER, new [] {agreementId, inactiveAgreementId})
        };
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns((true, Enumerable.Repeat<(Guid, CompanyApplicationStatusId, string?)>((applicationId, CompanyApplicationStatusId.CREATED, null), 1), companyRoleIds, Guid.NewGuid()));

        // Act
        async Task Act() => await _sut.Submit(data);

        // Assert
        var ex = await Assert.ThrowsAsync<ControllerArgumentException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_ARG_NOT_ACTIVE_AGREEMENTS.ToString());
    }

    [Fact]
    public async Task Submit_WithoutProcessId_ThrowsConflictException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();
        var agreementId1 = Guid.NewGuid();
        var data = new PartnerSubmitData(
            new[] { CompanyRoleId.APP_PROVIDER },
            new[]
            {
                new AgreementConsentData(agreementId, ConsentStatusId.ACTIVE),
                new AgreementConsentData(agreementId1, ConsentStatusId.ACTIVE),
            });
        var companyRoleIds = new (CompanyRoleId, IEnumerable<Guid>)[]
        {
            (CompanyRoleId.APP_PROVIDER, new [] {agreementId, agreementId1})
        };
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns((true, Enumerable.Repeat<(Guid, CompanyApplicationStatusId, string?)>((applicationId, CompanyApplicationStatusId.CREATED, "https://callback.url"), 1), companyRoleIds, null));

        // Act
        async Task Act() => await _sut.Submit(data);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_CONFLICT_PROCESS_MUST_EXIST.ToString());
    }

    [Fact]
    public async Task Submit_WithValidData_CallsExpected()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();
        var agreementId1 = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var submitProcessId = Guid.NewGuid();
        var processSteps = new List<ProcessStep<Process, ProcessTypeId, ProcessStepTypeId>>();
        var application = new CompanyApplication(applicationId, _identity.CompanyId, CompanyApplicationStatusId.CREATED, CompanyApplicationTypeId.EXTERNAL, DateTimeOffset.UtcNow);

        var data = new PartnerSubmitData(
            new[] { CompanyRoleId.APP_PROVIDER },
            new[]
            {
                new AgreementConsentData(agreementId, ConsentStatusId.ACTIVE),
                new AgreementConsentData(agreementId1, ConsentStatusId.ACTIVE),
            });
        var companyRoleIds = new (CompanyRoleId, IEnumerable<Guid>)[]
        {
            (CompanyRoleId.APP_PROVIDER, new [] {agreementId, agreementId1})
        };
        A.CallTo(() => _networkRepository.GetSubmitData(_identity.CompanyId))
            .Returns((true, Enumerable.Repeat<(Guid, CompanyApplicationStatusId, string?)>((applicationId, CompanyApplicationStatusId.CREATED, "https://callback.url"), 1), companyRoleIds, submitProcessId));
        A.CallTo(() => _applicationRepository.AttachAndModifyCompanyApplication(applicationId, A<Action<CompanyApplication>>._))
            .Invokes((Guid _, Action<CompanyApplication> setOptionalFields) =>
            {
                setOptionalFields.Invoke(application);
            });
        A.CallTo(() => _processStepRepository.CreateProcessStepRange(A<IEnumerable<(ProcessStepTypeId ProcessStepTypeId, ProcessStepStatusId ProcessStepStatusId, Guid ProcessId)>>._))
            .Invokes((IEnumerable<(ProcessStepTypeId ProcessStepTypeId, ProcessStepStatusId ProcessStepStatusId, Guid ProcessId)> steps) =>
                {
                    processSteps.AddRange(steps.Select(x => new ProcessStep<Process, ProcessTypeId, ProcessStepTypeId>(Guid.NewGuid(), x.ProcessStepTypeId, x.ProcessStepStatusId, x.ProcessId, DateTimeOffset.UtcNow)));
                });
        var consents = new List<Consent>();
        var now = DateTimeOffset.UtcNow;
        A.CallTo(() => _consentRepository.CreateConsents(A<IEnumerable<(Guid, Guid, Guid, ConsentStatusId)>>._))
            .Invokes((IEnumerable<(Guid AgreementId, Guid CompanyId, Guid CompanyUserId, ConsentStatusId ConsentStatusId)> agreementConsents) =>
            {
                foreach (var x in agreementConsents)
                {
                    consents.Add(new Consent(Guid.NewGuid(), x.AgreementId, x.CompanyId, x.CompanyUserId, x.ConsentStatusId, now));
                }
            });
        A.CallTo(() => _processStepRepository.CreateProcess(ProcessTypeId.APPLICATION_CHECKLIST))
            .ReturnsLazily((ProcessTypeId processTypeId) => new Process(processId, processTypeId, Guid.NewGuid()));
        A.CallTo(() => _checklistService.CreateInitialChecklistAsync(applicationId))
            .Returns(new[]
            {
                (ApplicationChecklistEntryTypeId.REGISTRATION_VERIFICATION, ApplicationChecklistEntryStatusId.TO_DO),
                (ApplicationChecklistEntryTypeId.BUSINESS_PARTNER_NUMBER, ApplicationChecklistEntryStatusId.DONE),
                (ApplicationChecklistEntryTypeId.IDENTITY_WALLET, ApplicationChecklistEntryStatusId.TO_DO),
                (ApplicationChecklistEntryTypeId.SELF_DESCRIPTION_LP, ApplicationChecklistEntryStatusId.TO_DO),
                (ApplicationChecklistEntryTypeId.CLEARING_HOUSE, ApplicationChecklistEntryStatusId.TO_DO),
                (ApplicationChecklistEntryTypeId.APPLICATION_ACTIVATION, ApplicationChecklistEntryStatusId.TO_DO),
            });
        A.CallTo(() => _checklistService.GetInitialProcessStepTypeIds(A<IEnumerable<(ApplicationChecklistEntryTypeId, ApplicationChecklistEntryStatusId)>>._))
            .Returns(new[] { ProcessStepTypeId.MANUAL_VERIFY_REGISTRATION, ProcessStepTypeId.MANUAL_DECLINE_APPLICATION });

        // Act
        await _sut.Submit(data);

        // Assert
        application.ApplicationStatusId.Should().Be(CompanyApplicationStatusId.SUBMITTED);
        A.CallTo(() => _consentRepository.CreateConsents(A<IEnumerable<(Guid, Guid, Guid, ConsentStatusId)>>._))
            .MustHaveHappenedOnceExactly();
        consents.Should().HaveCount(2)
            .And.AllSatisfy(x => x.Should().Match<Consent>(x =>
                x.CompanyId == _identity.CompanyId &&
                x.CompanyUserId == _identity.IdentityId &&
                x.ConsentStatusId == ConsentStatusId.ACTIVE))
            .And.Satisfy(
                x => x.AgreementId == agreementId,
                x => x.AgreementId == agreementId1);
        A.CallTo(() => _portalRepositories.SaveAsync())
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _processStepRepository.CreateProcessStepRange(A<IEnumerable<(ProcessStepTypeId ProcessStepTypeId, ProcessStepStatusId ProcessStepStatusId, Guid ProcessId)>>._))
            .MustHaveHappenedOnceExactly();
        processSteps.Should().Satisfy(
            x => x.ProcessId == processId && x.ProcessStepTypeId == ProcessStepTypeId.MANUAL_VERIFY_REGISTRATION && x.ProcessStepStatusId == ProcessStepStatusId.TODO,
            x => x.ProcessId == processId && x.ProcessStepTypeId == ProcessStepTypeId.MANUAL_DECLINE_APPLICATION && x.ProcessStepStatusId == ProcessStepStatusId.TODO,
            x => x.ProcessId == submitProcessId && x.ProcessStepTypeId == ProcessStepTypeId.TRIGGER_CALLBACK_OSP_SUBMITTED && x.ProcessStepStatusId == ProcessStepStatusId.TODO);
    }

    #endregion

    #region DeclineOsp

    [Fact]
    public async Task DeclineOsp_WithoutExisting_ThrowsNotFoundException()
    {
        // Arrange
        var notExistingId = Guid.NewGuid();
        var data = _fixture.Create<DeclineOspData>();
        A.CallTo(() => _networkRepository.GetDeclineDataForApplicationId(notExistingId, CompanyApplicationTypeId.EXTERNAL, A<IEnumerable<CompanyApplicationStatusId>>._, IdentityCompanyId))
            .Returns<(bool, bool, bool, bool,
                (
                    (CompanyStatusId, IEnumerable<(Guid, UserStatusId)>),
                    IEnumerable<(Guid, InvitationStatusId)>,
                    VerifyProcessData<ProcessTypeId, ProcessStepTypeId>
                )?)>(default);

        // Act
        async Task Act() => await _sut.DeclineOsp(notExistingId, data);

        // Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_COMPANY_APPLICATION_NOT_EXIST.ToString());
    }

    [Fact]
    public async Task DeclineOsp_WithWrongCompany_ThrowsForbiddenException()
    {
        // Arrange
        var applcationId = Guid.NewGuid();
        var data = _fixture.Create<DeclineOspData>();
        A.CallTo(() => _networkRepository.GetDeclineDataForApplicationId(applcationId, CompanyApplicationTypeId.EXTERNAL, A<IEnumerable<CompanyApplicationStatusId>>._, IdentityCompanyId))
            .Returns((true, true, true, false,
                default(
                    ((CompanyStatusId, IEnumerable<(Guid, UserStatusId)>),
                     IEnumerable<(Guid, InvitationStatusId)>,
                     VerifyProcessData<ProcessTypeId, ProcessStepTypeId>))));

        // Act
        async Task Act() => await _sut.DeclineOsp(applcationId, data);

        // Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_FORBIDDEN_USER_NOT_ALLOWED_DECLINE_APPLICATION.ToString());
    }

    [Fact]
    public async Task DeclineOsp_WithInternalApplication_ThrowsConflictException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var data = _fixture.Create<DeclineOspData>();
        A.CallTo(() => _networkRepository.GetDeclineDataForApplicationId(applicationId, CompanyApplicationTypeId.EXTERNAL, A<IEnumerable<CompanyApplicationStatusId>>._, IdentityCompanyId))
            .Returns((true, false, true, true,
                default(
                    ((CompanyStatusId, IEnumerable<(Guid, UserStatusId)>),
                     IEnumerable<(Guid, InvitationStatusId)>,
                     VerifyProcessData<ProcessTypeId, ProcessStepTypeId>))));

        // Act
        async Task Act() => await _sut.DeclineOsp(applicationId, data);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_CONFLICT_EXTERNAL_REGISTRATIONS_DECLINED.ToString());
    }

    [Fact]
    public async Task DeclineOsp_WithInvalidApplicationState_ThrowsConflictException()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var data = _fixture.Create<DeclineOspData>();
        A.CallTo(() => _networkRepository.GetDeclineDataForApplicationId(applicationId, CompanyApplicationTypeId.EXTERNAL, A<IEnumerable<CompanyApplicationStatusId>>._, IdentityCompanyId))
            .Returns((true, true, false, true,
                default(
                    ((CompanyStatusId, IEnumerable<(Guid, UserStatusId)>),
                      IEnumerable<(Guid, InvitationStatusId)>,
                      VerifyProcessData<ProcessTypeId, ProcessStepTypeId>))));

        // Act
        async Task Act() => await _sut.DeclineOsp(applicationId, data);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Act);
        ex.Message.Should().Be(NetworkErrors.NETWORK_CONFLICT_CHECK_APPLICATION_STATUS.ToString());
    }

    [Fact]
    public async Task DeclineOsp_WithValid_ExecutesExpected()
    {
        // Arrange
        var application = _fixture.Build<CompanyApplication>().With(x => x.ApplicationStatusId, CompanyApplicationStatusId.CREATED).Create();
        var company = new Company(IdentityCompanyId, "company", CompanyStatusId.ACTIVE, DateTimeOffset.UtcNow);
        var invitation = _fixture.Build<Invitation>().With(x => x.InvitationStatusId, InvitationStatusId.PENDING).Create();
        var identityId = Guid.NewGuid();
        var currentVersion = Guid.NewGuid();
        var process = _fixture.Build<Process>()
            .With(x => x.LockExpiryDate, default(DateTimeOffset?))
            .With(x => x.Version, currentVersion).Create();
        var currentProcessStep = new ProcessStep<Process, ProcessTypeId, ProcessStepTypeId>(Guid.NewGuid(), ProcessStepTypeId.MANUAL_DECLINE_OSP, ProcessStepStatusId.TODO, process.Id, DateTimeOffset.UtcNow);
        var removeUsersProcessStep = new ProcessStep<Process, ProcessTypeId, ProcessStepTypeId>(Guid.NewGuid(), ProcessStepTypeId.REMOVE_KEYCLOAK_USERS, ProcessStepStatusId.TODO, process.Id, DateTimeOffset.UtcNow);
        var otherProcessStep = new ProcessStep<Process, ProcessTypeId, ProcessStepTypeId>(Guid.NewGuid(), ProcessStepTypeId.SYNCHRONIZE_USER, ProcessStepStatusId.TODO, process.Id, DateTimeOffset.UtcNow);
        var existingProcessSteps = new[] { currentProcessStep, removeUsersProcessStep, otherProcessStep };
        var data = _fixture.Create<DeclineOspData>();
        A.CallTo(() => _networkRepository.GetDeclineDataForApplicationId(application.Id, CompanyApplicationTypeId.EXTERNAL, A<IEnumerable<CompanyApplicationStatusId>>._, IdentityCompanyId))
            .Returns((true, true, true, true,
                (
                    (company.CompanyStatusId, Enumerable.Repeat((identityId, UserStatusId.ACTIVE), 1)),
                    Enumerable.Repeat((invitation.Id, invitation.InvitationStatusId), 1),
                    new VerifyProcessData<ProcessTypeId, ProcessStepTypeId>(process, existingProcessSteps)
                )
            ));
        A.CallTo(() => _applicationRepository.AttachAndModifyCompanyApplication(application.Id, A<Action<CompanyApplication>>._))
            .Invokes((Guid _, Action<CompanyApplication> modify) =>
            {
                modify.Invoke(application);
            });
        A.CallTo(() => _companyRepository.AttachAndModifyCompany(company.Id, A<Action<Company>>._, A<Action<Company>>._))
            .Invokes((Guid _, Action<Company>? initialize, Action<Company> modify) =>
            {
                initialize?.Invoke(company);
                modify.Invoke(company);
            });
        A.CallTo(() => _invitationRepository.AttachAndModifyInvitations(A<IEnumerable<(Guid, Action<Invitation>?, Action<Invitation>)>>._))
            .Invokes((IEnumerable<(Guid InvitationId, Action<Invitation>? Initialize, Action<Invitation> Modify)> invitationData) =>
            {
                var initial = invitationData.Select(x =>
                    {
                        x.Initialize?.Invoke(invitation);
                        return (Invitation: invitation, modify: x.Modify);
                    }
                ).ToList();
                initial.ForEach(x => x.modify(x.Invitation));
            });
        A.CallTo(() => _processStepRepository.AttachAndModifyProcessSteps(A<IEnumerable<(Guid, Action<IProcessStep<ProcessStepTypeId>>?, Action<IProcessStep<ProcessStepTypeId>>)>>._))
            .Invokes((IEnumerable<(Guid ProcessStepId, Action<IProcessStep<ProcessStepTypeId>>? Initialize, Action<IProcessStep<ProcessStepTypeId>> Modify)> processSteps) =>
            {
                var initial = processSteps.Select(x =>
                    {
                        var existing = existingProcessSteps.Single(s => s.Id == x.ProcessStepId);
                        x.Initialize?.Invoke(existing);
                        return (ProcessStep: existing, modify: x.Modify);
                    }
                ).ToList();
                initial.ForEach(x => x.modify(x.ProcessStep));
            });

        // Act
        await _sut.DeclineOsp(application.Id, data);

        // Assert
        A.CallTo(() => _processStepRepository.CreateProcessStepRange(A<IEnumerable<(ProcessStepTypeId, ProcessStepStatusId, Guid)>>._))
            .MustNotHaveHappened();
        A.CallTo(() => _portalRepositories.SaveAsync())
            .MustHaveHappenedOnceExactly();
        application.ApplicationStatusId.Should().Be(CompanyApplicationStatusId.CANCELLED_BY_CUSTOMER);
        invitation.InvitationStatusId.Should().Be(InvitationStatusId.DECLINED);
        company.CompanyStatusId.Should().Be(CompanyStatusId.REJECTED);
        currentProcessStep.ProcessStepStatusId.Should().Be(ProcessStepStatusId.DONE);
        removeUsersProcessStep.ProcessStepStatusId.Should().Be(ProcessStepStatusId.TODO);
        otherProcessStep.ProcessStepStatusId.Should().Be(ProcessStepStatusId.SKIPPED);
        process.Version.Should().NotBe(currentVersion);
    }

    #endregion

    #region Setup

    private void SetupRepos()
    {
        A.CallTo(() => _networkRepository.CheckExternalIdExists(ExistingExternalId, A<Guid>.That.Matches(x => x == _identity.CompanyId || x == NoIdpCompanyId)))
            .Returns(true);
        A.CallTo(() => _networkRepository.CheckExternalIdExists(A<string>.That.Not.Matches(x => x == ExistingExternalId), A<Guid>.That.Matches(x => x == _identity.CompanyId || x == NoIdpCompanyId)))
            .Returns(false);

        A.CallTo(() => _companyRepository.CheckBpnExists(Bpnl)).Returns(false);
        A.CallTo(() => _companyRepository.CheckBpnExists(A<string>.That.Not.Matches(x => x == Bpnl))).Returns(true);

        A.CallTo(() => _countryRepository.CheckCountryExistsByAlpha2CodeAsync("XX"))
            .Returns(false);
        A.CallTo(() => _countryRepository.CheckCountryExistsByAlpha2CodeAsync(A<string>.That.Not.Matches(x => x == "XX")))
            .Returns(true);

        A.CallTo(() => _identityProviderRepository.GetSingleManagedIdentityProviderAliasDataUntracked(_identity.CompanyId))
            .Returns((IdpId, "test-alias"));

        A.CallTo(() => _identityProviderRepository.GetSingleManagedIdentityProviderAliasDataUntracked(NoAliasIdpCompanyId))
            .Returns((IdpId, null));

        A.CallTo(() => _identityProviderRepository.GetSingleManagedIdentityProviderAliasDataUntracked(NoIdpCompanyId))
            .Returns<(Guid, string?)>(default);

        A.CallTo(() => _identityProviderRepository.GetSingleManagedIdentityProviderAliasDataUntracked(MultiIdpCompanyId))
            .Throws(new InvalidOperationException("Sequence contains more than one element."));

        A.CallTo(() => _identityProviderRepository.GetManagedIdentityProviderAliasDataUntracked(A<Guid>.That.Matches(x => x == _identity.CompanyId || x == NoIdpCompanyId), A<IEnumerable<Guid>>._))
            .Returns(new (Guid, string?)[] { (IdpId, "test-alias") }.ToAsyncEnumerable());

        A.CallTo(() => _userProvisioningService.GetRoleDatas(A<IEnumerable<UserRoleConfig>>._))
            .Returns(new[] { new UserRoleData(UserRoleId, "cl1", "Company Admin") }.ToAsyncEnumerable());
    }

    #endregion
}
