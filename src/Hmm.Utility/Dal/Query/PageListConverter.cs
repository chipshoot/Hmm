using AutoMapper;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.Dal.Query
{
    /// <summary>
    /// AutoMapper converter for PageList types.
    /// Converts between PageList of different element types while preserving pagination metadata.
    /// </summary>
    /// <typeparam name="TIn">Source element type</typeparam>
    /// <typeparam name="TOut">Destination element type</typeparam>
    public class PageListConverter<TIn, TOut> : ITypeConverter<PageList<TIn>, PageList<TOut>>
    {
        /// <summary>
        /// Converts a PageList of one type to a PageList of another type.
        /// </summary>
        /// <param name="source">The source PageList</param>
        /// <param name="destination">The destination PageList (ignored, a new instance is created)</param>
        /// <param name="context">The AutoMapper resolution context</param>
        /// <returns>A new PageList with converted items and preserved pagination metadata</returns>
        public PageList<TOut> Convert(PageList<TIn> source, PageList<TOut> destination, ResolutionContext context)
        {
            if (source == null)
            {
                return new PageList<TOut>();
            }

            // Convert items using AutoMapper
            var sourceItems = source.ToList();
            var destItems = context.Mapper.Map<List<TIn>, List<TOut>>(sourceItems);

            // Create new PageList preserving pagination metadata
            return new PageList<TOut>(destItems, source.TotalCount, source.CurrentPage, source.PageSize);
        }
    }
}
