// Models/ApiResourceViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.ApiResources
{
    public class ViewModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string DisplayName { get; set; }
        
        [StringLength(1000)]
        public string Description { get; set; }
        
        public bool Enabled { get; set; } = true;
        
        public bool ShowInDiscoveryDocument { get; set; } = true;
        
        public bool RequireResourceIndicator { get; set; }
        
        [StringLength(100)]
        public string AllowedAccessTokenSigningAlgorithms { get; set; }

        // Helper property to convert the comma-separated string to a collection
        public IEnumerable<string> AllowedAlgorithms 
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AllowedAccessTokenSigningAlgorithms))
                    return new List<string>();
                
                return AllowedAccessTokenSigningAlgorithms
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(alg => alg.Trim());
            }
        } 

        public List<string> UserClaims { get; set; } = new List<string>();
        
        public List<string> Scopes { get; set; } = new List<string>();
    }
}
