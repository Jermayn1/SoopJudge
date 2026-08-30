using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
    {
        public void Configure(EntityTypeBuilder<Submission> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.ErrorMessage)
                .HasMaxLength(1000);

            builder.HasMany(s => s.Files)
                .WithOne(f => f.Submission)
                .HasForeignKey(f => f.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.EvaluationResult)
                .WithOne(e => e.Submission)
                .HasForeignKey<EvaluationResult>(e => e.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}