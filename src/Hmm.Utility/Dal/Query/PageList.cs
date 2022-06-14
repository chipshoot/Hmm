using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Query
{
    public class PageList<T> : List<T>
    {
        public PageList(IEnumerable<T> items, int count, int pageIndex, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public int CurrentPage { get; }

        public int TotalPages { get; }

        public int TotalCount { get; }

        public int PageSize { get; }

        public bool HasPrevPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;

        public static PageList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            Guard.Against<ArgumentOutOfRangeException>(pageIndex <= 0, nameof(pageIndex));
            Guard.Against<ArgumentOutOfRangeException>(pageSize < 1, nameof(pageSize));
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PageList<T>(items, count, pageIndex, pageSize);
        }

        public static async Task<PageList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            Guard.Against<ArgumentOutOfRangeException>(pageIndex <= 0, nameof(pageIndex));
            Guard.Against<ArgumentOutOfRangeException>(pageSize < 1, nameof(pageSize));

            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PageList<T>(items, count, pageIndex, pageSize);
        }
    }
}