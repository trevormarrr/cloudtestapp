using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CloudTestApp.Data;
using CloudTestApp.Models;

namespace CloudTestApp.Pages_Contacts
{
    public class IndexModel : PageModel
    {
        private readonly CloudTestApp.Data.AppDbContext _context;

        public IndexModel(CloudTestApp.Data.AppDbContext context)
        {
            _context = context;
        }

        public IList<Contact> Contact { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Contact = await _context.Contacts.ToListAsync();
        }
    }
}
