using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskUnitTestFileConfiguration : IEntityTypeConfiguration<TaskUnitTestFile>
    {
        public void Configure(EntityTypeBuilder<TaskUnitTestFile> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(f => f.Content)
                .IsRequired()
                .HasColumnType("text");
        }
    }
}
