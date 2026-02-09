using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    public class IndexModel : PageModel
    {
        private readonly Surveillance_MVP.Data.ApplicationDbContext _context;

        public IndexModel(Surveillance_MVP.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Patient> Patient { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Patient = await _context.Patients
                .Include(p => p.CountryOfBirth)
                .Include(p => p.Ethnicity)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.SexAtBirth)
                .Include(p => p.Occupation)
                .ToListAsync();
        }
    }
}
