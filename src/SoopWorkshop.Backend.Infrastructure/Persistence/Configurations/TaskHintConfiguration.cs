using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;


namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskHintConfiguration : IEntityTypeConfiguration<TaskHint>
    {
        public void Configure(EntityTypeBuilder<TaskHint> builder)
        {
            builder.HasKey(h => h.Id);

            builder.Property(h => h.Content)
                .IsRequired()
                .HasColumnType("text");
        }
    }
}