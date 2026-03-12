using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Simple contact list view
/// Matches vw_ContactsListSimple SQL view
/// </summary>
[Keyless]
public class ContactListSimple
{
    public Guid ContactId { get; set; }
    
    [Display(Name = "Contact Number")]
    public string? ContactNumber { get; set; }
    
    [Display(Name = "Date Identified")]
    public DateTime? DateIdentified { get; set; }
    
    [Display(Name = "Date of Onset")]
    public DateTime? ContactDateOfOnset { get; set; }
    
    [Display(Name = "Patient ID")]
    public Guid PatientId { get; set; }
    
    [Display(Name = "Name")]
    public string? ContactName { get; set; }
    
    [Display(Name = "First Name")]
    public string? ContactFirstName { get; set; }
    
    [Display(Name = "Last Name")]
    public string? ContactLastName { get; set; }
    
    [Display(Name = "DOB")]
    public DateTime? ContactDOB { get; set; }
    
    [Display(Name = "Mobile")]
    public string? ContactMobile { get; set; }
    
    [Display(Name = "Email")]
    public string? ContactEmail { get; set; }
    
    [Display(Name = "Suburb")]
    public string? ContactSuburb { get; set; }
    
    [Display(Name = "State")]
    public string? ContactState { get; set; }
    
    [Display(Name = "Disease")]
    public string? DiseaseName { get; set; }
    
    [Display(Name = "Exposure Type")]
    public string? ExposureType { get; set; }
    
    [Display(Name = "Exposure Source")]
    public string? ExposureSourceName { get; set; }
    
    [Display(Name = "Total Tasks")]
    public int TotalTasks { get; set; }
    
    [Display(Name = "Completed Tasks")]
    public int CompletedTasks { get; set; }
    
    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; }
    
    [Display(Name = "Updated At")]
    public DateTime UpdatedAt { get; set; }
}
