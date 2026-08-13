using Estud.Back.Domain.Campi;
using Estud.Back.Domain.Calendar;

namespace Estud.Back.Database.Calendar;

public class CalendarDayDbConfig : IEntityTypeConfiguration<CalendarDay>
{
    public void Configure(EntityTypeBuilder<CalendarDay> entity)
    {
        entity.ToTable("calendar_days", DbSchemas.Estud);

        entity.HasKey(e => e.Id);

        entity.HasOne<Campus>()
            .WithMany()
            .HasForeignKey(e => e.CampusId);

        entity.HasIndex(e => new { e.InstitutionId, e.CampusId, e.Date })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}
