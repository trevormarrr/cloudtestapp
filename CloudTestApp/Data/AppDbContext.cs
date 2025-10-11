using Microsoft.EntityFrameworkCore;
using CloudTestApp.Models;

namespace CloudTestApp.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Contact> Contacts => Set<Contact>();
    }
}
