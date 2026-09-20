using Estud.Back.Domain.Classes;

namespace Estud.Back.Database.Classes;

public class ClassActivityWorkEntryDbConfig : IEntityTypeConfiguration<ClassActivityWorkEntry>
{
    public void Configure(EntityTypeBuilder<ClassActivityWorkEntry> entity)
    {
        entity.ToTable("class_activity_work_entries", DbSchemas.Estud);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Content).HasMaxLength(10000);

        entity.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);

        entity.HasIndex(e => new { e.ClassActivityWorkId, e.CreatedAt });
    }
}
