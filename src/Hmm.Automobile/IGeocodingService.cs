using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IGeocodingService
    {
        Task<ProcessingResult<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude);
    }
}
