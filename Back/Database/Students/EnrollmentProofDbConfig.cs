using Newtonsoft.Json;
using Estud.Back.Domain.Students;

namespace Estud.Back.Database.Students;

public class EnrollmentProofDbConfig : IEntityTypeConfiguration<EnrollmentProof>
{
    public void Configure(EntityTypeBuilder<EnrollmentProof> entity)
    {
        entity.ToTable("enrollment_proofs", DbSchemas.Estud);

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        entity.Property(e => e.Metadata)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<EnrollmentProofMetadata>(v) ?? new()
            )
            .HasColumnType("jsonb")
            .IsRequired();

        entity.HasOne<EstudStudent>()
            .WithMany()
            .HasForeignKey(e => e.StudentId);

        entity.HasIndex(e => e.Code).IsUnique();
    }
}
