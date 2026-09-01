using Estud.Back.Domain.Identity;

namespace Estud.Back.Database.Identity;

public class SsoAllowedDomainDbConfig : IEntityTypeConfiguration<SsoAllowedDomain>
{
    public void Configure(EntityTypeBuilder<SsoAllowedDomain> entity)
    {
        entity.ToTable("sso_allowed_domains", DbSchemas.Estud);

        entity.HasKey(e => e.Domain);
        entity.Property(e => e.Domain).ValueGeneratedNever();

        entity.Property(e => e.VerificationToken).HasMaxLength(64);
        entity.Property(e => e.LastError).HasMaxLength(500);

        entity.Ignore(e => e.IsVerified);
        entity.Ignore(e => e.RecordName);
        entity.Ignore(e => e.RecordValue);

        entity.HasIndex(e => new { e.Status, e.LastCheckedAt });

        entity.HasOne(e => e.Configuration)
            .WithMany(c => c.AllowedDomains)
            .HasPrincipalKey(c => c.Id)
            .HasForeignKey(e => e.SsoConfigurationId);
    }
}
