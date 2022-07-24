using System;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Hmm.Core.Dal.EF.Repositories;

public static class RepositoryHelperExtensions
{
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderBy)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrEmpty(orderBy))
        {
            return source;
        }

        var orderBys = orderBy.Split(',');
        var orderByString = string.Empty;
        var propInfos = typeof(T).GetProperties();
        foreach (var orderClause in orderBys.Reverse())
        {
            var trimmedClause = orderClause.Trim();
            var idxOfSpace = trimmedClause.IndexOf(" ", StringComparison.Ordinal);
            var propName = idxOfSpace == -1 ? trimmedClause : trimmedClause.Remove(idxOfSpace);
            if (propInfos.Any(p => p.Name == propName))
            {
                orderByString += (string.IsNullOrEmpty(orderByString) ? string.Empty : ", ") + trimmedClause;
            }
        }

        return source.OrderBy(orderByString);
    }
}