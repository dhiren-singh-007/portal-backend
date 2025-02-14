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

using FluentAssertions;
using RestAssured.Response.Logging;
using Xunit;
using Xunit.Abstractions;
using static RestAssured.Dsl;

namespace Org.Eclipse.TractusX.Portal.Backend.EndToEnd.Tests;

[Trait("Category", "PortalHC")]
[TestCaseOrderer("Org.Eclipse.TractusX.Portal.Backend.EndToEnd.Tests.AlphabeticalOrderer", "Org.Eclipse.TractusX.Portal.Backend.EndToEnd.Tests")]
[Collection("PortalHC")]
public class BaseDataLoadCheck : EndToEndTestBase, IAsyncLifetime
{
    private const string EndPoint = "/api/administration";

    private static readonly string BaseUrl = TestResources.BasePortalBackendUrl;
    private static readonly Secrets Secrets = new();
    private static readonly string PortalUserCompanyName = TestResources.PortalUserCompanyName;
    private string? _portalUserToken;

    public BaseDataLoadCheck(ITestOutputHelper output) : base(output)
    {
    }

    public async Task InitializeAsync()
    {
        _portalUserToken = await new AuthFlow(PortalUserCompanyName).GetAccessToken(Secrets.PortalUserName,
            Secrets.PortalUserPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // GET: /api/administration/staticdata/usecases
    [Fact]
    public async Task GetUseCaseData()
    {
        _portalUserToken.Should().NotBeNullOrEmpty("Token for the portal user could not be fetched correctly");

        var response = Given()
            .DisableSslCertificateValidation()
            .Header(
                "authorization",
                $"Bearer {_portalUserToken}")
            .When()
            .Get($"{BaseUrl}{EndPoint}/staticdata/usecases")
            .Then()
            .Log(ResponseLogLevel.OnError)
            .StatusCode(200)
            .Extract()
            .Response();

        var result = await response.Content.ReadAsStringAsync();

        result.Should().NotBeNullOrEmpty("Error: Response body is null or empty");
    }

    //     GET: /api/administration/staticdata/languagetags
    [Fact]
    public async Task GetAppLanguageTags()
    {
        _portalUserToken.Should().NotBeNullOrEmpty("Token for the portal user could not be fetched correctly");

        var response = Given()
            .DisableSslCertificateValidation()
            .Header(
                "authorization",
                $"Bearer {_portalUserToken}")
            .When()
            .Get($"{BaseUrl}{EndPoint}/staticdata/languagetags")
            .Then()
            .Log(ResponseLogLevel.OnError)
            .StatusCode(200)
            .Extract()
            .Response();

        var result = await response.Content.ReadAsStringAsync();
        result.Should().NotBeNullOrEmpty("Response body is null or empty");
    }

    //     GET: /api/administration/staticdata/licenseType
    [Fact]
    public async Task GetAllLicenseTypes()
    {
        _portalUserToken.Should().NotBeNullOrEmpty("Token for the portal user could not be fetched correctly");

        var response = Given()
            .DisableSslCertificateValidation()
            .Header(
                "authorization",
                $"Bearer {_portalUserToken}")
            .When()
            .Get($"{BaseUrl}{EndPoint}/staticdata/licenseType")
            .Then()
            .Log(ResponseLogLevel.OnError)
            .StatusCode(200)
            .Extract()
            .Response();

        var result = await response.Content.ReadAsStringAsync();
        result.Should().NotBeNullOrEmpty("Response body is null or empty");
    }

    //     GET: api/administration/user/owncompany/users
    [Fact]
    public async Task GetCompanyUserData()
    {
        _portalUserToken.Should().NotBeNullOrEmpty("Token for the portal user could not be fetched correctly");

        var response = Given()
            .DisableSslCertificateValidation()
            .Header(
                "authorization",
                $"Bearer {_portalUserToken}")
            .When()
            .Get($"{BaseUrl}{EndPoint}/user/owncompany/users?page=0&size=5")
            .Then()
            .Log(ResponseLogLevel.OnError)
            .StatusCode(200)
            .Extract()
            .Response();

        var result = await response.Content.ReadAsStringAsync();
        result.Should().NotBeNullOrEmpty("Response body is null or empty");
    }

    //     GET: api/administration/companydata/ownCompanyDetails
    [Fact]
    public async Task GetOwnCompanyDetails()
    {
        _portalUserToken.Should().NotBeNullOrEmpty("Token for the portal user could not be fetched correctly");

        var response = Given()
            .DisableSslCertificateValidation()
            .Header(
                "authorization",
                $"Bearer {_portalUserToken}")
            .When()
            .Get($"{BaseUrl}{EndPoint}/companydata/ownCompanyDetails")
            .Then()
            .Log(ResponseLogLevel.OnError)
            .StatusCode(200)
            .Extract()
            .Response();

        var result = await response.Content.ReadAsStringAsync();
        result.Should().NotBeNullOrEmpty("Response body is null or empty");
    }
}
