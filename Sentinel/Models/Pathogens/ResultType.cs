using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Pathogens
{
    /// <summary>
    /// Type of result that can be reported for a pathogen test
    /// </summary>
    public enum ResultType
    {
        [Display(Name = "Qualitative (Positive/Negative)")]
        Qualitative = 1,

        [Display(Name = "Quantitative (Numeric)")]
        Quantitative = 2,

        [Display(Name = "Semi-Quantitative (Titer)")]
        Semiquantitative = 3,

        [Display(Name = "Both Qualitative and Quantitative")]
        Both = 4
    }
}
