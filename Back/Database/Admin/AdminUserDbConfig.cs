using Estud.Back.Domain.Admin;
using Estud.Back.Domain.Identity;

namespace Estud.Back.Database.Admin;

public class AdminUserDbConfig : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> entity)
    {
        entity.ToTable("admin_users", DbSchemas.Estud);

        entity.HasKey(e => e.UserId);
        entity.Property(e => e.UserId).ValueGeneratedNever();

        entity.HasOne<EstudUser>()
            .WithMany()
            .HasPrincipalKey(u => u.Id)
            .HasForeignKey(e => e.UserId);
    }
}
