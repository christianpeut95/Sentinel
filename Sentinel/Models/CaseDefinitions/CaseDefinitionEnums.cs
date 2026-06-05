using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.CaseDefinitions
{
    public enum CaseDefinitionStatus
    {
        [Display(Name = "Draft")]
        Draft = 1,

        [Display(Name = "Current")]
        Current = 2,

        [Display(Name = "Archived")]
        Archived = 3
    }

    public enum CriterionType
    {
        [Display(Name = "Clinical")]
        Clinical = 1,

        [Display(Name = "Laboratory")]
        Laboratory = 2,

        [Display(Name = "Epidemiological")]
        Epidemiological = 3,

        [Display(Name = "Demographic")]
        Demographic = 4,

        [Display(Name = "Custom Field")]
        CustomField = 5
    }

    public enum LogicalOperator
    {
        [Display(Name = "AND")]
        AND = 1,

        [Display(Name = "OR")]
        OR = 2,

        [Display(Name = "NOT")]
        NOT = 3
    }

    public enum ComparisonOperator
    {
        [Display(Name = "Equals")]
        Equals = 1,

        [Display(Name = "Not Equals")]
        NotEquals = 2,

        [Display(Name = "Contains")]
        Contains = 3,

        [Display(Name = "Does Not Contain")]
        DoesNotContain = 4,

        [Display(Name = "Greater Than")]
        GreaterThan = 5,

        [Display(Name = "Less Than")]
        LessThan = 6,

        [Display(Name = "Between")]
        Between = 7,

        [Display(Name = "In List")]
        InList = 8,

        [Display(Name = "Is Present")]
        IsPresent = 9,

        [Display(Name = "Is Absent")]
        IsAbsent = 10
    }

    public enum RecommendedAction
    {
        [Display(Name = "None")]
        None = 0,

        [Display(Name = "Auto Classify")]
        AutoClassify = 1,

        [Display(Name = "Suggest Classification")]
        SuggestClassification = 2,

        [Display(Name = "Flag for Review")]
        FlagForReview = 3
    }
}
