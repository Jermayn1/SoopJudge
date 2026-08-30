using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskExpectedMethodConfiguration : IEntityTypeConfiguration<TaskExpectedMethod>
    {
        public void Configure(EntityTypeBuilder<TaskExpectedMethod> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Signature)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}
