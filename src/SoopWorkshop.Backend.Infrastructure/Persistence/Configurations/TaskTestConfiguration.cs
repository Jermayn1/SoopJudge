using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskTestConfiguration : IEntityTypeConfiguration<TaskTest>
    {
        public void Configure(EntityTypeBuilder<TaskTest> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Input)
                .HasColumnType("text");

            builder.Property(t => t.ExpectedOutput)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(500);
        }
    }
}