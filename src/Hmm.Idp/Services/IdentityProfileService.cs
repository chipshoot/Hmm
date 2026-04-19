// Services/ProfileService.cs
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace Hmm.Idp.Services
{
    public class IdentityProfileService : IProfileService
    {
        private readonly IApplicationUserRepository _userRepository;

        public IdentityProfileService(IApplicationUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var claims = await _userRepository.GetClaimsBySubjectIdAsync(context.Subject.GetSubjectId());
            context.IssuedClaims.AddRange(claims);
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var user = await _userRepository.FindBySubjectIdAsync(context.Subject.GetSubjectId());
            context.IsActive = user != null && user.IsActive;
        }
    }
}