using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TestCaseComparisonConfiguration : IEntityTypeConfiguration<TestCaseComparison>
    {
        public void Configure(EntityTypeBuilder<TestCaseComparison> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Call)
                .HasColumnType("text");

            builder.Property(c => c.Expected)
                .HasColumnType("text");

            builder.Property(c => c.Actual)
                .HasColumnType("text");

            builder.Property(c => c.Order);
        }
    }
}
