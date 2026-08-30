using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
    {
        public void Configure(EntityTypeBuilder<SubmissionFile> builder)
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