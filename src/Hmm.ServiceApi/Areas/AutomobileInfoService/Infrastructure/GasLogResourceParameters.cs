using Hmm.Utility.Dal.Query;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;

public class GasLogResourceParameters : ResourceCollectionParameters
{
    public int AutomobileId { get; set; }
}