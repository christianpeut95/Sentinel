using System.Collections.Concurrent;
using Sentinel.Models.Telemetry;

namespace Sentinel.Services.Telemetry
{
    /// <summary>
    /// Thread-safe singleton service for tracking usage activity during reporting periods
    /// </summary>
    public class ActivityTracker
    {
        private readonly ConcurrentDictionary<string, int> _pageViewCounts = new();
        private readonly ConcurrentDictionary<string, bool> _uniqueUsers = new();
        private int _loginSuccessful;
        private int _loginFailed;
        private int _casesCreated;
        private int _patientsCreated;
        private int _outbreaksCreated;
        private int _labResultsCreated;
        private int _exposuresCreated;
        private int _surveysCreated;
        private int _surveysCompleted;
        private int _hl7Processed;
        private int _hl7Succeeded;
        private int _hl7Failed;
        private int _reportsGenerated;
        private int _customFieldsCreated;
        private DateTime _periodStart = DateTime.UtcNow;

        /// <summary>
        /// Track a page view for a given sanitized route
        /// </summary>
        public void TrackPageView(string route)
        {
            _pageViewCounts.AddOrUpdate(route, 1, (_, count) => count + 1);
        }

        /// <summary>
        /// Track a login attempt
        /// </summary>
        public void TrackLogin(bool success, string? userId = null)
        {
            if (success)
            {
                Interlocked.Increment(ref _loginSuccessful);
                if (!string.IsNullOrEmpty(userId))
                {
                    _uniqueUsers.TryAdd(userId, true);
                }
            }
            else
            {
                Interlocked.Increment(ref _loginFailed);
            }
        }

        /// <summary>
        /// Track case creation
        /// </summary>
        public void TrackCaseCreated()
        {
            Interlocked.Increment(ref _casesCreated);
        }

        /// <summary>
        /// Track patient creation
        /// </summary>
        public void TrackPatientCreated()
        {
            Interlocked.Increment(ref _patientsCreated);
        }

        /// <summary>
        /// Track outbreak creation
        /// </summary>
        public void TrackOutbreakCreated()
        {
            Interlocked.Increment(ref _outbreaksCreated);
        }

        /// <summary>
        /// Track lab result creation
        /// </summary>
        public void TrackLabResultCreated()
        {
            Interlocked.Increment(ref _labResultsCreated);
        }

        /// <summary>
        /// Track exposure creation
        /// </summary>
        public void TrackExposureCreated()
        {
            Interlocked.Increment(ref _exposuresCreated);
        }

        /// <summary>
        /// Track survey creation
        /// </summary>
        public void TrackSurveyCreated()
        {
            Interlocked.Increment(ref _surveysCreated);
        }

        /// <summary>
        /// Track survey completion
        /// </summary>
        public void TrackSurveyCompleted()
        {
            Interlocked.Increment(ref _surveysCompleted);
        }

        /// <summary>
        /// Track HL7 file processing
        /// </summary>
        public void TrackHL7Processed(bool success)
        {
            Interlocked.Increment(ref _hl7Processed);
            if (success)
            {
                Interlocked.Increment(ref _hl7Succeeded);
            }
            else
            {
                Interlocked.Increment(ref _hl7Failed);
            }
        }

        /// <summary>
        /// Track report generation
        /// </summary>
        public void TrackReportGenerated()
        {
            Interlocked.Increment(ref _reportsGenerated);
        }

        /// <summary>
        /// Track custom field creation
        /// </summary>
        public void TrackCustomFieldCreated()
        {
            Interlocked.Increment(ref _customFieldsCreated);
        }

        /// <summary>
        /// Get the current activity report and reset counters for next period
        /// </summary>
        public ActivityReport GetActivityReportAndReset()
        {
            var report = new ActivityReport
            {
                PageViews = _pageViewCounts
                    .Select(kvp => new PageViewCount { Page = kvp.Key, Count = kvp.Value })
                    .OrderByDescending(p => p.Count)
                    .ToList(),
                Logins = new LoginActivity
                {
                    Successful = _loginSuccessful,
                    Failed = _loginFailed,
                    UniqueActiveUsers = _uniqueUsers.Count
                },
                CasesCreated = _casesCreated,
                PatientsCreated = _patientsCreated,
                OutbreaksCreated = _outbreaksCreated,
                LabResultsCreated = _labResultsCreated,
                ExposuresCreated = _exposuresCreated,
                Surveys = new SurveyActivity
                {
                    Created = _surveysCreated,
                    Completed = _surveysCompleted
                },
                Hl7 = new HL7Activity
                {
                    MessagesProcessed = _hl7Processed,
                    MessagesSucceeded = _hl7Succeeded,
                    MessagesFailed = _hl7Failed
                },
                Reports = new ReportActivity
                {
                    Generated = _reportsGenerated
                },
                CustomFields = new CustomFieldActivity
                {
                    Created = _customFieldsCreated
                }
            };

            // Reset counters for next period
            _pageViewCounts.Clear();
            _uniqueUsers.Clear();
            _loginSuccessful = 0;
            _loginFailed = 0;
            _casesCreated = 0;
            _patientsCreated = 0;
            _outbreaksCreated = 0;
            _labResultsCreated = 0;
            _exposuresCreated = 0;
            _surveysCreated = 0;
            _surveysCompleted = 0;
            _hl7Processed = 0;
            _hl7Succeeded = 0;
            _hl7Failed = 0;
            _reportsGenerated = 0;
            _customFieldsCreated = 0;
            _periodStart = DateTime.UtcNow;

            return report;
        }

        /// <summary>
        /// Get the start time of the current reporting period
        /// </summary>
        public DateTime GetPeriodStart()
        {
            return _periodStart;
        }
    }
}
