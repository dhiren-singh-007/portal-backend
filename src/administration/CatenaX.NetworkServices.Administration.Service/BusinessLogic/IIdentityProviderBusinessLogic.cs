using CatenaX.NetworkServices.Administration.Service.Models;
using CatenaX.NetworkServices.Provisioning.Library.Enums;

namespace CatenaX.NetworkServices.Administration.Service.BusinessLogic;

public interface IIdentityProviderBusinessLogic
{
    IAsyncEnumerable<IdentityProviderDetails> GetOwnCompanyIdentityProviders(string iamUserId);
    Task<IdentityProviderDetails> CreateOwnCompanyIdentityProvider(IamIdentityProviderProtocol protocol, string iamUserId);
    Task<IdentityProviderDetails> UpdateOwnCompanyIdentityProvider(Guid identityProviderId, IdentityProviderEditableDetails details, string iamUserId);
    IAsyncEnumerable<UserIdentityProviderData> GetOwnCompanyUserIdentityProviderDataAsync(IEnumerable<string> aliase, string iamUserId);
}
