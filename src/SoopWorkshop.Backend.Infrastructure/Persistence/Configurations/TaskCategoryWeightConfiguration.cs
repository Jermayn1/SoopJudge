using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskCategoryWeightConfiguration : IEntityTypeConfiguration<TaskCategoryWeight>
    {
        public void Configure(EntityTypeBuilder<TaskCategoryWeight> builder)
        {
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Weight)
                .IsRequired();

            // Zwei Gewichte für dieselbe Kategorie einer Aufgabe wären
            // widersprüchlich - die Datenbank lässt das gar nicht erst zu.
            builder.HasIndex(w => new { w.TaskItemId, w.Category })
                .IsUnique();
        }
    }
}
