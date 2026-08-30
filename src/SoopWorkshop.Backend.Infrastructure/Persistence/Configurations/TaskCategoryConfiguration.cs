using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskCategoryConfiguration : IEntityTypeConfiguration<TaskCategory>
    {

        public void Configure(EntityTypeBuilder<TaskCategory> entityTypeBuilder)
        {
            entityTypeBuilder.HasKey(c => c.Id);

            entityTypeBuilder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            // Reichlich Platz für einen Symbolnamen; die längsten in der
            // Sammlung haben rund 20 Zeichen.
            entityTypeBuilder.Property(c => c.IconName)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue(string.Empty);

            entityTypeBuilder.HasMany(c => c.Tasks)
                .WithOne(t => t.Category)
                .HasForeignKey(t => t.TaskCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}