using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Delete")]
    public class DeleteModel : PageModel
    {
        private readonly Sentinel.Data.ApplicationDbContext _context;

        public DeleteModel(Sentinel.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Patient Patient { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(m => m.Id == id);

            if (patient is not null)
            {
                Patient = patient;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Invalid patient ID.";
                return RedirectToPage("./Index");
            }

            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient != null)
                {
                    var patientName = $"{patient.GivenName} {patient.FamilyName}";
                    await _context.SoftDeleteAsync(patient);
                    TempData["SuccessMessage"] = $"Patient {patientName} has been deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Patient not found. It may have already been deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the patient: {ex.Message}";
            }

            return RedirectToPage("./Index");
        }
    }
}
