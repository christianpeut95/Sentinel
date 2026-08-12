using System.Collections.Concurrent;
using Sentinel.Models.Telemetry;

namespace Sentinel.Services.Telemetry
{
    /// <summary>
    /// Singleton service that tracks user navigation and actions for error context breadcrumbs
    /// </summary>
    public class BreadcrumbTracker
    {
        private readonly ConcurrentQueue<Breadcrumb> _breadcrumbs = new();
        private const int MaxBreadcrumbs = 20;

        /// <summary>
        /// Add a navigation breadcrumb (page view)
        /// </summary>
        public void AddNavigationBreadcrumb(string route)
        {
            AddBreadcrumb("Navigation", route);
        }

        /// <summary>
        /// Add a command/action breadcrumb
        /// </summary>
        public void AddCommandBreadcrumb(string action)
        {
            AddBreadcrumb("Command", action);
        }

        /// <summary>
        /// Add a generic breadcrumb with custom category and event
        /// </summary>
        public void AddBreadcrumb(string category, string eventName)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(eventName))
                return;

            var breadcrumb = new Breadcrumb
            {
                TimestampUtc = DateTime.UtcNow,
                Category = category,
                Event = eventName
            };

            _breadcrumbs.Enqueue(breadcrumb);

            // Trim to max capacity (keep most recent)
            while (_breadcrumbs.Count > MaxBreadcrumbs)
            {
                _breadcrumbs.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Get recent breadcrumbs for error reporting
        /// </summary>
        public List<Breadcrumb> GetRecentBreadcrumbs()
        {
            return _breadcrumbs.ToList();
        }

        /// <summary>
        /// Clear all breadcrumbs (useful for testing or session reset)
        /// </summary>
        public void Clear()
        {
            while (_breadcrumbs.TryDequeue(out _)) { }
        }
    }
}
