using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Pathogens
{
    /// <summary>
    /// Category classification for pathogens/biomarkers
    /// </summary>
    public enum PathogenCategory
    {
        [Display(Name = "Antigen")]
        Antigen = 1,

        [Display(Name = "Antibody - IgM")]
        Antibody_IgM = 2,

        [Display(Name = "Antibody - IgG")]
        Antibody_IgG = 3,

        [Display(Name = "Antibody - IgA")]
        Antibody_IgA = 4,

        [Display(Name = "Antibody - Total")]
        Antibody_Total = 5,

        [Display(Name = "Nucleic Acid - RNA")]
        NucleicAcid_RNA = 6,

        [Display(Name = "Nucleic Acid - DNA")]
        NucleicAcid_DNA = 7,

        [Display(Name = "Culture")]
        Culture = 8,

        [Display(Name = "Toxin")]
        Toxin = 9,

        [Display(Name = "Enzyme")]
        Enzyme = 10,

        [Display(Name = "Other")]
        Other = 99
    }
}
