using Hmm.Utility.Misc;

namespace Hmm.ServiceApi.DtoEntity.Services
{
    public interface IPropertyCheckService
    {
        bool TypeHasProperties<T>(string fields);

        ProcessingResult ProcessingResult { get; }
    }
}