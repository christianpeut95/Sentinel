using Sentinel.Models.CaseDefinitions;
using System.Globalization;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Evaluates comparison operations between actual and expected values
    /// </summary>
    public class OperatorEvaluator
    {
        /// <summary>
        /// Evaluates a comparison operation
        /// </summary>
        /// <param name="actualValue">The actual value from the case data</param>
        /// <param name="expectedValue">The expected value from the criterion</param>
        /// <param name="operator">The comparison operator to apply</param>
        /// <returns>True if the comparison passes, false otherwise</returns>
        public bool Evaluate(object? actualValue, string? expectedValue, ComparisonOperator @operator)
        {
            // Handle null/empty checks first
            if (@operator == ComparisonOperator.IsPresent)
            {
                return IsValuePresent(actualValue);
            }

            if (@operator == ComparisonOperator.IsAbsent)
            {
                return !IsValuePresent(actualValue);
            }

            // If actualValue is null for other operators, return false
            if (actualValue == null)
            {
                return false;
            }

            // Convert to strings for comparison
            string actualStr = actualValue.ToString() ?? string.Empty;
            string expectedStr = expectedValue ?? string.Empty;

            return @operator switch
            {
                ComparisonOperator.Equals => EvaluateEquals(actualStr, expectedStr),
                ComparisonOperator.NotEquals => !EvaluateEquals(actualStr, expectedStr),
                ComparisonOperator.Contains => EvaluateContains(actualStr, expectedStr),
                ComparisonOperator.DoesNotContain => !EvaluateContains(actualStr, expectedStr),
                ComparisonOperator.GreaterThan => EvaluateGreaterThan(actualStr, expectedStr),
                ComparisonOperator.LessThan => EvaluateLessThan(actualStr, expectedStr),
                ComparisonOperator.Between => EvaluateBetween(actualStr, expectedStr),
                ComparisonOperator.InList => EvaluateInList(actualStr, expectedStr),
                _ => false
            };
        }

        private bool IsValuePresent(object? value)
        {
            if (value == null) return false;
            if (value is string str) return !string.IsNullOrWhiteSpace(str);
            return true;
        }

        private bool EvaluateEquals(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private bool EvaluateContains(string actual, string expected)
        {
            return actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
        }

        private bool EvaluateGreaterThan(string actual, string expected)
        {
            // Try numeric comparison first
            if (TryParseNumeric(actual, out var actualNum) && TryParseNumeric(expected, out var expectedNum))
            {
                return actualNum > expectedNum;
            }

            // Try date comparison
            if (DateTime.TryParse(actual, out var actualDate) && DateTime.TryParse(expected, out var expectedDate))
            {
                return actualDate > expectedDate;
            }

            // Fall back to string comparison
            return string.Compare(actual, expected, StringComparison.Ordinal) > 0;
        }

        private bool EvaluateLessThan(string actual, string expected)
        {
            // Try numeric comparison first
            if (TryParseNumeric(actual, out var actualNum) && TryParseNumeric(expected, out var expectedNum))
            {
                return actualNum < expectedNum;
            }

            // Try date comparison
            if (DateTime.TryParse(actual, out var actualDate) && DateTime.TryParse(expected, out var expectedDate))
            {
                return actualDate < expectedDate;
            }

            // Fall back to string comparison
            return string.Compare(actual, expected, StringComparison.Ordinal) < 0;
        }

        private bool EvaluateBetween(string actual, string expected)
        {
            // Expected format: "min,max"
            var parts = expected.Split(',');
            if (parts.Length != 2)
            {
                return false;
            }

            string min = parts[0].Trim();
            string max = parts[1].Trim();

            // Try numeric comparison
            if (TryParseNumeric(actual, out var actualNum) && 
                TryParseNumeric(min, out var minNum) && 
                TryParseNumeric(max, out var maxNum))
            {
                return actualNum >= minNum && actualNum <= maxNum;
            }

            // Try date comparison
            if (DateTime.TryParse(actual, out var actualDate) && 
                DateTime.TryParse(min, out var minDate) && 
                DateTime.TryParse(max, out var maxDate))
            {
                return actualDate >= minDate && actualDate <= maxDate;
            }

            return false;
        }

        private bool EvaluateInList(string actual, string expected)
        {
            // Expected can be comma-separated or JSON array
            if (expected.StartsWith("[") && expected.EndsWith("]"))
            {
                // Try parse as JSON array
                try
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(expected);
                    if (list != null)
                    {
                        return list.Any(item => string.Equals(item, actual, StringComparison.OrdinalIgnoreCase));
                    }
                }
                catch
                {
                    // Fall through to comma-separated
                }
            }

            // Comma-separated list
            var items = expected.Split(',').Select(s => s.Trim());
            return items.Any(item => string.Equals(item, actual, StringComparison.OrdinalIgnoreCase));
        }

        private bool TryParseNumeric(string value, out decimal result)
        {
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }
}
