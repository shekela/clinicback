using ClinicBack.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBack.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Times> Times { get; set; }
        public DbSet<Admin> Admins { get; set; }

    }
}
