using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskCategory> TaskCategories => Set<TaskCategory>();
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();
        public DbSet<TaskHint> TaskHints => Set<TaskHint>();
        public DbSet<TaskTest> TaskTests => Set<TaskTest>();
        public DbSet<TaskUnitTestFile> TaskUnitTestFiles => Set<TaskUnitTestFile>();
        public DbSet<TaskExpectedType> TaskExpectedTypes => Set<TaskExpectedType>();
        public DbSet<TaskExpectedMethod> TaskExpectedMethods => Set<TaskExpectedMethod>();
        public DbSet<TaskCategoryWeight> TaskCategoryWeights => Set<TaskCategoryWeight>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
        public DbSet<EvaluationResult> EvaluationResults => Set<EvaluationResult>();
        public DbSet<CategoryResult> CategoryResults => Set<CategoryResult>();
        public DbSet<TestCaseResult> TestCaseResults => Set<TestCaseResult>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Läd automatisch alle Config-Klassen
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}