using AutoMapper;
using Hmm.Utility.Misc;

namespace Hmm.Core.Map
{
    /// <summary>
    /// Extension methods for IMapper that provide null-safe mapping with ProcessingResult.
    /// Eliminates duplicate null check patterns across all managers.
    /// </summary>
    public static class MapperExtensions
    {
        /// <summary>
        /// Maps a source object to a destination type and returns a ProcessingResult.
        /// Returns a Fail result if the mapping produces null.
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TDest">The destination type</typeparam>
        /// <param name="mapper">The AutoMapper instance</param>
        /// <param name="source">The source object to map</param>
        /// <returns>ProcessingResult containing the mapped object or an error if mapping failed</returns>
        public static ProcessingResult<TDest> MapWithNullCheck<TSource, TDest>(
            this IMapper mapper,
            TSource source)
        {
            if (source == null)
            {
                return ProcessingResult<TDest>.Fail($"Cannot map null {typeof(TSource).Name} to {typeof(TDest).Name}");
            }

            var result = mapper.Map<TDest>(source);
            if (result == null)
            {
                return ProcessingResult<TDest>.Fail($"Cannot convert {typeof(TSource).Name} to {typeof(TDest).Name}");
            }

            return ProcessingResult<TDest>.Ok(result);
        }

        /// <summary>
        /// Maps a source object to a destination type and returns a ProcessingResult.
        /// Returns a Fail result if the mapping produces null.
        /// Overload that allows specifying a custom error message.
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TDest">The destination type</typeparam>
        /// <param name="mapper">The AutoMapper instance</param>
        /// <param name="source">The source object to map</param>
        /// <param name="errorMessage">Custom error message if mapping fails</param>
        /// <returns>ProcessingResult containing the mapped object or an error if mapping failed</returns>
        public static ProcessingResult<TDest> MapWithNullCheck<TSource, TDest>(
            this IMapper mapper,
            TSource source,
            string errorMessage)
        {
            if (source == null)
            {
                return ProcessingResult<TDest>.Fail(errorMessage);
            }

            var result = mapper.Map<TDest>(source);
            if (result == null)
            {
                return ProcessingResult<TDest>.Fail(errorMessage);
            }

            return ProcessingResult<TDest>.Ok(result);
        }

        /// <summary>
        /// Maps a source object to a destination type.
        /// Returns a Fail result if source is null, otherwise returns Ok with the mapped result (which may be null for collections).
        /// Use this for mappings where a null result is acceptable (e.g., empty collections).
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TDest">The destination type</typeparam>
        /// <param name="mapper">The AutoMapper instance</param>
        /// <param name="source">The source object to map</param>
        /// <returns>ProcessingResult containing the mapped object</returns>
        public static ProcessingResult<TDest> MapWithSourceNullCheck<TSource, TDest>(
            this IMapper mapper,
            TSource source)
        {
            if (source == null)
            {
                return ProcessingResult<TDest>.Fail($"Cannot map null {typeof(TSource).Name} to {typeof(TDest).Name}");
            }

            var result = mapper.Map<TDest>(source);
            return ProcessingResult<TDest>.Ok(result);
        }
    }
}
