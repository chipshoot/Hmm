using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Utility.Dal.Query
{
    /// <summary>
    /// A read-only paginated list that wraps a collection of items with pagination metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class uses composition instead of inheritance from <see cref="List{T}"/> to avoid
    /// exposing unnecessary mutable methods. It implements <see cref="IReadOnlyList{T}"/> to
    /// provide read-only indexed access and enumeration.
    /// </para>
    /// <para>
    /// Benefits of this design:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Read-only: No Add, Remove, Clear, or other mutation methods exposed</description></item>
    ///   <item><description>Encapsulated: Internal storage is completely hidden</description></item>
    ///   <item><description>Safe: Cannot accidentally modify a paginated result</description></item>
    ///   <item><description>Clean API: Only exposes what's needed for pagination scenarios</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    public class PageList<T> : IReadOnlyList<T>
    {
        private readonly List<T> _items;

        /// <summary>
        /// Creates an empty PageList with default pagination values.
        /// </summary>
        public PageList()
        {
            _items = [];
            TotalCount = 0;
            PageSize = 1;
            CurrentPage = 0;
            TotalPages = 1;
        }

        /// <summary>
        /// Creates a PageList with the specified items and pagination metadata.
        /// </summary>
        /// <param name="items">The items for this page</param>
        /// <param name="count">The total count of items across all pages</param>
        /// <param name="pageIndex">The current page index (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        public PageList(IEnumerable<T> items, int count, int pageIndex, int pageSize)
        {
            _items = items?.ToList() ?? [];
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        }

        /// <summary>
        /// Gets the current page number (1-based).
        /// </summary>
        public int CurrentPage { get; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Gets the total count of items across all pages.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets whether there is a previous page available.
        /// </summary>
        public bool HasPrevPage => CurrentPage > 1;

        /// <summary>
        /// Gets whether there is a next page available.
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Gets the number of items in this page.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gets the item at the specified index within this page.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get</param>
        /// <returns>The item at the specified index</returns>
        public T this[int index] => _items[index];

        /// <summary>
        /// Returns an enumerator that iterates through the items in this page.
        /// </summary>
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the items in this page.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Creates a PageList synchronously from a queryable source.
        /// </summary>
        /// <param name="source">The queryable source</param>
        /// <param name="pageIndex">The page index (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns>A PageList containing the requested page of items</returns>
        public static PageList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PageList<T>(items, count, pageIndex, pageSize);
        }

        /// <summary>
        /// Creates a PageList asynchronously from a queryable source.
        /// </summary>
        /// <param name="source">The queryable source</param>
        /// <param name="pageIndex">The page index (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns>A task that resolves to a PageList containing the requested page of items</returns>
        public static async Task<PageList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PageList<T>(items, count, pageIndex, pageSize);
        }
    }
}