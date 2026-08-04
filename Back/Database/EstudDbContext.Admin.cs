using Estud.Back.Domain.Admin;
using Estud.Back.Database.Admin;

namespace Estud.Back.Database;

public partial class EstudDbContext
{
    public DbSet<AdminUser> AdminUsers { get; set; }

    private static void ConfigureAdmin(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AdminUserDbConfig());
    }
}
