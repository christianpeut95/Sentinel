using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.CaseDefinitions
{
    /// <summary>
    /// Defines how matched HL7 data should be stored when multiple synonymous values exist
    /// </summary>
    public enum DataStoragePreference
    {
        [Display(Name = "Store as received from HL7")]
        StoreAsReceived = 1,

        [Display(Name = "Always store as canonical value")]
        StoreAsCanonical = 2
    }
}
