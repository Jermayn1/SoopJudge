using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TestCaseResultConfiguration : IEntityTypeConfiguration<TestCaseResult>
    {
        public void Configure(EntityTypeBuilder<TestCaseResult> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(t => t.Input)
                .HasColumnType("text");

            builder.Property(t => t.ExpectedOutput)
                .HasColumnType("text");

            builder.Property(t => t.ActualOutput)
                .HasColumnType("text");

            // Sortierschlüssel der Anzeige - ohne Index, es sind wenige Zeilen
            // je Kategorie und sie werden ohnehin immer zusammen geladen.
            builder.Property(t => t.Order);
        }
    }
}