using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    public interface IGeocodingService
    {
        Task<ProcessingResult<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude);
    }
}
