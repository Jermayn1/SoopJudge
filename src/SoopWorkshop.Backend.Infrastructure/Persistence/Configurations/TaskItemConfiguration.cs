using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasColumnType("text");

            builder.HasMany(t => t.Hints)
                .WithOne(h => h.Task)
                .HasForeignKey(h => h.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Tests)
                .WithOne(test => test.Task)
                .HasForeignKey(test => test.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.ExpectedTypes)
                .WithOne(type => type.Task)
                .HasForeignKey(type => type.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.UnitTestFiles)
                .WithOne(file => file.Task)
                .HasForeignKey(file => file.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Submissions)
                .WithOne(s => s.Task)
                .HasForeignKey(s => s.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.CategoryWeights)
                .WithOne(weight => weight.Task)
                .HasForeignKey(weight => weight.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}