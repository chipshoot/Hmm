using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.Dal.Exceptions
{
    public static class ExceptionHelpExtensions
    {
        public static string GetAllMessage(this Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var messages = exception.FromHierarchy(ex => ex.InnerException)
                .Select(ex => ex.Message);

            return string.Join(Environment.NewLine, messages);
        }

        private static IEnumerable<TSource> FromHierarchy<TSource>(
            this TSource source,
            Func<TSource, TSource> nextItem) where TSource : class
        {
            return FromHierarchy(source, nextItem, s => s != null);
        }

        private static IEnumerable<TSource> FromHierarchy<TSource>(
            this TSource source,
            Func<TSource, TSource> nextItem,
            Func<TSource, bool> canContinue)
        {
            for (var current = source; canContinue(current); current = nextItem(current))
            {
                yield return current;
            }
        }
    }
}