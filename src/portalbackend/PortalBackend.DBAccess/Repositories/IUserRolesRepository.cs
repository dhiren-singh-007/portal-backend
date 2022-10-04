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

using Org.CatenaX.Ng.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.CatenaX.Ng.Portal.Backend.PortalBackend.PortalEntities.Entities;

namespace Org.CatenaX.Ng.Portal.Backend.PortalBackend.DBAccess.Repositories;

public interface IUserRolesRepository
{
    CompanyUserAssignedRole CreateCompanyUserAssignedRole(Guid companyUserId, Guid companyUserRoleId);
    IAsyncEnumerable<CompanyUser> GetCompanyUserRolesIamUsersAsync(IEnumerable<Guid> companyUserIds, string iamUserId);
    IAsyncEnumerable<(Guid CompanyUserId, Guid UserRoleId)> GetExistingRolesByNameForUserAsync(IEnumerable<string> roleNames, string iamUserId);

    CompanyUserAssignedRole RemoveCompanyUserAssignedRole(CompanyUserAssignedRole companyUserAssignedRole);
    IAsyncEnumerable<UserRoleData> GetUserRoleDataUntrackedAsync(IEnumerable<Guid> userRoleIds);
    IAsyncEnumerable<Guid> GetUserRoleIdsUntrackedAsync(IDictionary<string, IEnumerable<string>> clientRoles);
    IAsyncEnumerable<UserRoleWithId> GetUserRoleWithIdsUntrackedAsync(string clientClientId, IEnumerable<string> userRoles);
    IAsyncEnumerable<UserRoleData> GetUserRoleDataUntrackedAsync(IDictionary<string, IEnumerable<string>> clientRoles);
    IAsyncEnumerable<(string Role,Guid Id)> GetUserRolesWithIdAsync(string keyCloakClientId);
    IAsyncEnumerable<string> GetClientRolesCompositeAsync(string keyCloakClientId);
    IAsyncEnumerable<UserRoleWithDescription> GetServiceAccountRolesAsync(string clientId,string? languageShortName = null);
    
    /// <summary>
    /// Gets all user role ids for the given offerId
    /// </summary>
    /// <param name="offerId">Id of the offer the roles are assigned to.</param>
    /// <returns>Returns a list of user role ids</returns>
    Task<List<string>> GetUserRolesForOfferIdAsync(Guid offerId);

    IAsyncEnumerable<CompanyUserRoleDeletionData> GetAssignedRolesForDeletion(Guid companyUserId, IEnumerable<string> userRoles);
    IAsyncEnumerable<UserRoleWithId> GetRolesToAdd(string clientClientId, Guid companyUserId, IEnumerable<string> userRoles);
}
