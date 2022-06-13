namespace Hmm.Utility.Dal.Query
{
    public static class QueryHelpExtensions
    {
        public static (int pageIndex, int pageSize) GetPaginationTuple(this ResourceCollectionParameters parameter)
        {
            switch (parameter)
            {
                case null:
                {
                    var defaultParam = new ResourceCollectionParameters();
                    return (defaultParam.PageNumber, defaultParam.PageSize);
                }
                default:
                    return (parameter.PageNumber, parameter.PageSize);
            }
        }
        
    }
}