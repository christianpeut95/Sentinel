using Sentinel.Models.CaseDefinitions;
using Sentinel.Services.CaseDefinitionEvaluation;
using Xunit;

namespace Sentinel.Tests.Services.CaseDefinitionEvaluation
{
    public class OperatorEvaluatorTests
    {
        private readonly OperatorEvaluator _evaluator;

        public OperatorEvaluatorTests()
        {
            _evaluator = new OperatorEvaluator();
        }

        #region Equals Tests

        [Fact]
        public void Evaluate_Equals_StringMatch_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("test", "test", ComparisonOperator.Equals);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_Equals_CaseInsensitive_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("TEST", "test", ComparisonOperator.Equals);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_Equals_NoMatch_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("test", "other", ComparisonOperator.Equals);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_NotEquals_NoMatch_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("test", "other", ComparisonOperator.NotEquals);
            Assert.True(result);
        }

        #endregion

        #region Contains Tests

        [Fact]
        public void Evaluate_Contains_Substring_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("This is a test", "test", ComparisonOperator.Contains);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_Contains_CaseInsensitive_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("This is a TEST", "test", ComparisonOperator.Contains);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_Contains_NoMatch_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("This is a test", "missing", ComparisonOperator.Contains);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_DoesNotContain_NoMatch_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("This is a test", "missing", ComparisonOperator.DoesNotContain);
            Assert.True(result);
        }

        #endregion

        #region Numeric Comparison Tests

        [Fact]
        public void Evaluate_GreaterThan_Numbers_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("10", "5", ComparisonOperator.GreaterThan);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_GreaterThan_Decimals_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("10.5", "10.2", ComparisonOperator.GreaterThan);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_GreaterThan_NumbersEqual_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("10", "10", ComparisonOperator.GreaterThan);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_LessThan_Numbers_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("5", "10", ComparisonOperator.LessThan);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_LessThan_Decimals_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("10.2", "10.5", ComparisonOperator.LessThan);
            Assert.True(result);
        }

        #endregion

        #region Date Comparison Tests

        [Fact]
        public void Evaluate_GreaterThan_Dates_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("2024-01-15", "2024-01-10", ComparisonOperator.GreaterThan);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_LessThan_Dates_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("2024-01-10", "2024-01-15", ComparisonOperator.LessThan);
            Assert.True(result);
        }

        #endregion

        #region Between Tests

        [Fact]
        public void Evaluate_Between_Numbers_WithinRange_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("15", "10,20", ComparisonOperator.Between);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_Between_Numbers_AtLowerBound_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("10", "10,20", ComparisonOperator.Between);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_Between_Numbers_AtUpperBound_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("20", "10,20", ComparisonOperator.Between);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_Between_Numbers_OutsideRange_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("25", "10,20", ComparisonOperator.Between);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_Between_Dates_WithinRange_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("2024-01-15", "2024-01-10,2024-01-20", ComparisonOperator.Between);
            Assert.True(result);
        }

        #endregion

        #region InList Tests

        [Fact]
        public void Evaluate_InList_CommaSeparated_Found_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("apple", "apple,banana,orange", ComparisonOperator.InList);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_InList_CommaSeparated_CaseInsensitive_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("APPLE", "apple,banana,orange", ComparisonOperator.InList);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_InList_CommaSeparated_NotFound_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("grape", "apple,banana,orange", ComparisonOperator.InList);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_InList_JsonArray_Found_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("apple", "[\"apple\",\"banana\",\"orange\"]", ComparisonOperator.InList);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_InList_JsonArray_NotFound_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("grape", "[\"apple\",\"banana\",\"orange\"]", ComparisonOperator.InList);
            Assert.False(result);
        }

        #endregion

        #region IsPresent/IsAbsent Tests

        [Fact]
        public void Evaluate_IsPresent_NonNullValue_ReturnsTrue()
        {
            var result = _evaluator.Evaluate("test", null, ComparisonOperator.IsPresent);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_IsPresent_NullValue_ReturnsFalse()
        {
            var result = _evaluator.Evaluate(null, null, ComparisonOperator.IsPresent);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_IsPresent_EmptyString_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("", null, ComparisonOperator.IsPresent);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_IsPresent_WhitespaceString_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("   ", null, ComparisonOperator.IsPresent);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_IsAbsent_NullValue_ReturnsTrue()
        {
            var result = _evaluator.Evaluate(null, null, ComparisonOperator.IsAbsent);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_IsAbsent_NonNullValue_ReturnsFalse()
        {
            var result = _evaluator.Evaluate("test", null, ComparisonOperator.IsAbsent);
            Assert.False(result);
        }

        #endregion

        #region Null Handling Tests

        [Fact]
        public void Evaluate_NullActualValue_Equals_ReturnsFalse()
        {
            var result = _evaluator.Evaluate(null, "test", ComparisonOperator.Equals);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_NullActualValue_Contains_ReturnsFalse()
        {
            var result = _evaluator.Evaluate(null, "test", ComparisonOperator.Contains);
            Assert.False(result);
        }

        [Fact]
        public void Evaluate_NullActualValue_GreaterThan_ReturnsFalse()
        {
            var result = _evaluator.Evaluate(null, "10", ComparisonOperator.GreaterThan);
            Assert.False(result);
        }

        #endregion

        #region Type Conversion Tests

        [Fact]
        public void Evaluate_Equals_IntegerToString_ReturnsTrue()
        {
            var result = _evaluator.Evaluate(42, "42", ComparisonOperator.Equals);
            Assert.True(result);
        }

        [Fact]
        public void Evaluate_GreaterThan_IntegerObject_ReturnsTrue()
        {
            var result = _evaluator.Evaluate(15, "10", ComparisonOperator.GreaterThan);
            Assert.True(result);
        }

        #endregion
    }
}
