namespace Hmm.Utility.Dal.Query
{
    public class ResourceCollectionParameters
    {
        private int _pageSize = 10;
        private int _pageNumber = 1;

        public const int MaxPageSize = 100;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value <= 0 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string OrderBy { get; set; }
    }
}