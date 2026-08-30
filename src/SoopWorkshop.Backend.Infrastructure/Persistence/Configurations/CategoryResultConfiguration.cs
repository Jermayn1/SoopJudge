using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class CategoryResultConfiguration : IEntityTypeConfiguration<CategoryResult>
    {
        public void Configure(EntityTypeBuilder<CategoryResult> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.ErrorTip)
                .HasColumnType("text");

            builder.HasMany(c => c.TestCaseResults)
                .WithOne(t => t.CategoryResult)
                .HasForeignKey(t => t.CategoryResultId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}