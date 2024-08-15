using AutoMapper;
using Hmm.Utility.Dal.Query;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.Profiles;

public class PageListConverter<TIn, TOut> : ITypeConverter<PageList<TIn>, PageList<TOut>>
{
    public PageList<TOut> Convert(PageList<TIn> source, PageList<TOut> destination, ResolutionContext context)
    {
        var dest = context.Mapper.Map<List<TIn>, List<TOut>>(source);
        var result = new PageList<TOut>(dest, source.TotalCount, source.CurrentPage, source.PageSize);
        return result;
    }
}