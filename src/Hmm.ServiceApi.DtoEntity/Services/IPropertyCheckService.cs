using Hmm.Utility.Misc;

namespace Hmm.ServiceApi.DtoEntity.Services
{
    public interface IPropertyCheckService
    {
        ProcessingResult<bool> TypeHasProperties<T>(string fields);
    }
}