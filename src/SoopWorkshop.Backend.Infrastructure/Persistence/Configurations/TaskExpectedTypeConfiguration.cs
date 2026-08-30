using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskExpectedTypeConfiguration : IEntityTypeConfiguration<TaskExpectedType>
    {
        public void Configure(EntityTypeBuilder<TaskExpectedType> builder)
        {
            builder.HasKey(type => type.Id);

            // Gleiche Länge wie früher ExpectedClassName auf der Aufgabe.
            builder.Property(type => type.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasMany(type => type.Methods)
                .WithOne(method => method.Type)
                .HasForeignKey(method => method.TaskExpectedTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
