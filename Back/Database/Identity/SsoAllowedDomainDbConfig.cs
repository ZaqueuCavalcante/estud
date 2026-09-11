using Estud.Back.Domain.Identity;

namespace Estud.Back.Database.Identity;

public class SsoAllowedDomainDbConfig : IEntityTypeConfiguration<SsoAllowedDomain>
{
    public void Configure(EntityTypeBuilder<SsoAllowedDomain> entity)
    {
        entity.ToTable("sso_allowed_domains", DbSchemas.Estud);

        entity.HasKey(e => e.Id);

        entity.HasOne(e => e.SsoConfiguration)
            .WithMany(c => c.AllowedDomains)
            .HasPrincipalKey(c => c.Id)
            .HasForeignKey(e => e.SsoConfigurationId);

        entity.HasIndex(e => new { e.SsoConfigurationId, e.Domain })
            .IsUnique();

        entity.HasIndex(e => e.Domain)
            .IsUnique()
            .HasFilter($"status = {SsoDomainStatus.Verified.ToInt()}");
    }
}
