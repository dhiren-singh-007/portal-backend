/********************************************************************************
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

using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using Org.Eclipse.TractusX.Portal.Backend.Provisioning.Library.Enums;
using Org.Eclipse.TractusX.Portal.Backend.Provisioning.Library.Models;
using System.Text.Json.Serialization;

namespace Org.Eclipse.TractusX.Portal.Backend.Administration.Service.Models;

public record IdentityProviderDetails(
    Guid IdentityProviderId,
    string? Alias,
    IdentityProviderCategoryId IdentityProviderCategoryId,
    IdentityProviderTypeId IdentityProviderTypeId,
    string? DisplayName,
    string? RedirectUrl,
    bool? Enabled,
    IEnumerable<IdentityProviderMapperModel>? Mappers)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IdentityProviderDetailsOidc? Oidc { get; init; } = null;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IdentityProviderDetailsSaml? Saml { get; init; } = null;
}

public record IdentityProviderDetailsOidc(
    string? MetadataUrl,
    string? AuthorizationUrl,
    string? TokenUrl,
    string? LogoutUrl,
    string? ClientId,
    bool HasClientSecret,
    IamIdentityProviderClientAuthMethod? ClientAuthMethod,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IamIdentityProviderSignatureAlgorithm? SignatureAlgorithm
);

public record IdentityProviderDetailsSaml(
    string? ServiceProviderEntityId,
    string? SingleSignOnServiceUrl
);
