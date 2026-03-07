using Microsoft.EntityFrameworkCore;

namespace Sentinel.Data
{
    /// <summary>
    /// Extension methods for bypassing global query filters when needed.
    /// </summary>
    public static class QueryFilterExtensions
    {
        /// <summary>
        /// Bypasses the disease access filter. Use only for:
        /// - Administrative operations that need to see all diseases
        /// - Background jobs that run without a user context
        /// - System operations that require full access
        /// </summary>
        /// <remarks>
        /// SECURITY WARNING: This bypasses disease access control. Only use in trusted code paths.
        /// </remarks>
        public static IQueryable<T> BypassDiseaseAccessFilter<T>(this IQueryable<T> query) where T : class
        {
            return query.IgnoreQueryFilters();
        }

        /// <summary>
        /// Gets all cases including those with restricted diseases.
        /// Use only for administrative or system operations.
        /// </summary>
        public static IQueryable<Models.Case> IncludeRestrictedDiseaseCases(this IQueryable<Models.Case> query)
        {
            return query.IgnoreQueryFilters();
        }

        /// <summary>
        /// Gets all diseases including restricted ones.
        /// Use only for administrative disease management operations.
        /// </summary>
        public static IQueryable<Models.Lookups.Disease> IncludeRestrictedDiseases(this IQueryable<Models.Lookups.Disease> query)
        {
            return query.IgnoreQueryFilters();
        }
    }
}
