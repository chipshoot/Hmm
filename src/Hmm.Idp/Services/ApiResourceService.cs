// Services/ApiResourceService.cs
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Hmm.Idp.Models;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Services
{
    public class ApiResourceService
    {
        private readonly ConfigurationDbContext _context;

        public ApiResourceService(ConfigurationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ApiResourceViewModel>> GetAllApiResourcesAsync()
        {
            var apiResources = await _context.ApiResources
                .Include(x => x.UserClaims)
                .Include(x => x.Scopes)
                .ToListAsync();

            return apiResources.Select(ar => new ApiResourceViewModel
            {
                Name = ar.Name,
                DisplayName = ar.DisplayName,
                Description = ar.Description,
                Enabled = ar.Enabled,
                ShowInDiscoveryDocument = ar.ShowInDiscoveryDocument,
                RequireResourceIndicator = ar.RequireResourceIndicator,
                AllowedAccessTokenSigningAlgorithms = ar.AllowedAccessTokenSigningAlgorithms,
                UserClaims = ar.UserClaims.Select(c => c.Type).ToList(),
                Scopes = ar.Scopes.Select(s => s.Scope).ToList()
            }).ToList();
        }

        public async Task<ApiResourceViewModel> GetApiResourceByNameAsync(string name)
        {
            var apiResource = await _context.ApiResources
                .Include(x => x.UserClaims)
                .Include(x => x.Scopes)
                .FirstOrDefaultAsync(x => x.Name == name);

            if (apiResource == null)
                return null;

            return new ApiResourceViewModel
            {
                Name = apiResource.Name,
                DisplayName = apiResource.DisplayName,
                Description = apiResource.Description,
                Enabled = apiResource.Enabled,
                ShowInDiscoveryDocument = apiResource.ShowInDiscoveryDocument,
                RequireResourceIndicator = apiResource.RequireResourceIndicator,
                AllowedAccessTokenSigningAlgorithms = apiResource.AllowedAccessTokenSigningAlgorithms,
                UserClaims = apiResource.UserClaims.Select(c => c.Type).ToList(),
                Scopes = apiResource.Scopes.Select(s => s.Scope).ToList()
            };
        }

        public async Task CreateApiResourceAsync(ApiResourceViewModel model)
        {
            var apiResource = new ApiResource
            {
                Name = model.Name,
                DisplayName = model.DisplayName,
                Description = model.Description,
                Enabled = model.Enabled,
                ShowInDiscoveryDocument = model.ShowInDiscoveryDocument,
                RequireResourceIndicator = model.RequireResourceIndicator
            };

            if (!string.IsNullOrWhiteSpace(model.AllowedAccessTokenSigningAlgorithms))
            {
                apiResource.AllowedAccessTokenSigningAlgorithms = 
                    model.AllowedAccessTokenSigningAlgorithms
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(alg => alg.Trim())
                        .ToList();
            }

            // Add user claims
            if (model.UserClaims != null && model.UserClaims.Any())
            {
                foreach (var claim in model.UserClaims)
                {
                    apiResource.UserClaims.Add(claim);
                }
            }

            // Add scopes
            if (model.Scopes != null && model.Scopes.Any())
            {
                foreach (var scope in model.Scopes)
                {
                    apiResource.Scopes.Add(scope);
                }
            }

            _context.ApiResources.Add(apiResource.ToEntity());
            await _context.SaveChangesAsync();
        }

        public async Task UpdateApiResourceAsync(ApiResourceViewModel model)
        {
            var apiResource = await _context.ApiResources
                .Include(x => x.UserClaims)
                .Include(x => x.Scopes)
                .FirstOrDefaultAsync(x => x.Name == model.Name);

            if (apiResource == null)
                throw new Exception($"API Resource '{model.Name}' not found");

            apiResource.DisplayName = model.DisplayName;
            apiResource.Description = model.Description;
            apiResource.Enabled = model.Enabled;
            apiResource.ShowInDiscoveryDocument = model.ShowInDiscoveryDocument;
            apiResource.RequireResourceIndicator = model.RequireResourceIndicator;
            apiResource.AllowedAccessTokenSigningAlgorithms = model.AllowedAccessTokenSigningAlgorithms;
            apiResource.Updated = DateTime.UtcNow;

            // Update user claims
            apiResource.UserClaims.Clear();
            if (model.UserClaims != null)
            {
                foreach (var claim in model.UserClaims)
                {
                    apiResource.UserClaims.Add(new Duende.IdentityServer.EntityFramework.Entities.ApiResourceClaim
                    {
                        Type = claim,
                        ApiResourceId = apiResource.Id
                    });
                }
            }

            // Update scopes
            apiResource.Scopes.Clear();
            if (model.Scopes != null)
            {
                foreach (var scope in model.Scopes)
                {
                    apiResource.Scopes.Add(new Duende.IdentityServer.EntityFramework.Entities.ApiResourceScope
                    {
                        Scope = scope,
                        ApiResourceId = apiResource.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteApiResourceAsync(string name)
        {
            var apiResource = await _context.ApiResources
                .FirstOrDefaultAsync(x => x.Name == name);

            if (apiResource != null)
            {
                _context.ApiResources.Remove(apiResource);
                await _context.SaveChangesAsync();
            }
        }
    }
}
