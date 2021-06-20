using System.Collections.Generic;
using IdentityServer4.Models;

namespace WebApplicationExternalAuth.Configurations
{
    internal static class ClientStore
    {
        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource()
                {
                    Name = "someapi",
                    DisplayName = "Some API",
                    Description = "Just some resource",
                    Scopes = new List<string> {"some.read", "some.write"},
                    ApiSecrets = new List<Secret> {new Secret("ScopeSecretString".Sha256())},
                    UserClaims = new List<string> {"role"}
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource
                {
                    Name = "role",
                    UserClaims = new List<string>{"role"}
                }
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientName = "Some Api Client",
                    ClientId = "some_api_client_id",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                    ClientSecrets = new List<Secret> {new Secret("some_api_secret".Sha256())},
                    AllowedScopes =
                    {
                        "some.read",
                        "some.write"
                    },
                    AllowOfflineAccess = true,
                    AccessTokenLifetime = 3000
                }
            };
        }
        
        public static IEnumerable<ApiScope> GetApiScopes()
        {
            return new List<ApiScope>
            {
                new ApiScope("some.read", "Read access"),
                new ApiScope("some.write", "Write access"),
            };
        }
    }
}