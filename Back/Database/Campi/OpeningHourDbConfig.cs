using Estud.Back.Domain.Campi;

namespace Estud.Back.Database.Campi;

public class OpeningHourDbConfig : IEntityTypeConfiguration<OpeningHour>
{
    public void Configure(EntityTypeBuilder<OpeningHour> entity)
    {
        entity.ToTable("opening_hours", DbSchemas.Estud);

        entity.HasKey(e => e.Id);

        entity.HasOne(e => e.Campus)
            .WithMany(c => c.OpeningHours)
            .HasForeignKey(e => e.CampusId);

        entity.HasIndex(e => new { e.CampusId, e.Day });
    }
}
