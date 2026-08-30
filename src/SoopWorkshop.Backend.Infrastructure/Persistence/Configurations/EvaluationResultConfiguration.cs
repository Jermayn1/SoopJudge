using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class EvaluationResultConfiguration : IEntityTypeConfiguration<EvaluationResult>
    {
        public void Configure(EntityTypeBuilder<EvaluationResult> builder)
        {
            builder.HasKey(e => e.Id);

            builder.HasMany(e => e.CategoryResults)
                .WithOne(c => c.EvaluationResult)
                .HasForeignKey(c => c.EvaluationResultId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}