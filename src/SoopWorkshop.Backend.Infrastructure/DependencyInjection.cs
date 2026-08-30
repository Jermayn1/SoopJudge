using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Infrastructure.Configuration;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using SoopWorkshop.Backend.Infrastructure.Persistence.Repositories;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Infrastructure.Evaluation;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Backend.Infrastructure.Processes;
using SoopWorkshop.Backend.Application.Transfer.Interfaces;
using SoopWorkshop.Backend.Infrastructure.Transfer;

namespace SoopWorkshop.Backend.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Es ist kein Connection-String gesetzt." + Environment.NewLine +
                    "Lokal: .env im Repository-Wurzelverzeichnis anlegen (Vorlage: .env.example) und" + Environment.NewLine +
                    "POSTGRES_PASSWORD setzen - der Connection-String wird daraus gebaut." + Environment.NewLine +
                    "Im Betrieb ueber die Umgebungsvariable ConnectionStrings__DefaultConnection.");
            }

            services.Configure<EvaluationOptions>(
                configuration.GetSection(EvaluationOptions.SectionName));

            services.Configure<DatabaseOptions>(
                configuration.GetSection(DatabaseOptions.SectionName));

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Sagt, ob die Datenbank wirklich antwortet - der Healthcheck im
            // Compose-Aufbau hängt daran. Ein Backend, das steht, aber nicht an
            // seine Datenbank kommt, ist nicht "bereit".
            services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("datenbank");

            // VOR dem EvaluationWorker: dessen erste Handlung ist ein Zugriff auf
            // die Datenbank, und dieser Dienst hält den Start auf, bis das Schema
            // steht. Die Reihenfolge der Registrierung ist die Reihenfolge des
            // Starts - sie ist hier keine Kosmetik.
            services.AddHostedService<DatabaseMigrationService>();

            services.AddScoped<ITaskCategoryRepository, TaskCategoryRepository>();
            services.AddScoped<ITaskItemRepository, TaskItemRepository>();
            services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            services.AddScoped<IEvaluationResultRepository, EvaluationResultRepository>();
            services.AddScoped<ITaskTestRepository, TaskTestRepository>();
            services.AddScoped<ITaskUnitTestFileRepository, TaskUnitTestFileRepository>();
            services.AddScoped<ITaskCategoryWeightRepository, TaskCategoryWeightRepository>();

            services.AddScoped<IProcessRunner, ProcessRunner>();

            // Singleton, weil sich Einreihen (Request-Scope) und Abarbeiten
            // (Hintergrunddienst) dieselbe Warteschlange teilen müssen.
            services.AddSingleton<IEvaluationQueue, EvaluationQueue>();
            services.AddHostedService<EvaluationWorker>();

            // Reihenfolge hier ist egal - der JavaAnalyzer sortiert nach
            // IEvaluationChecker.Order. Eine neue Prüfung wird nur ergänzt.
            services.AddScoped<IEvaluationChecker, ContractChecker>();
            services.AddScoped<IEvaluationChecker, CompilabilityChecker>();
            services.AddScoped<IEvaluationChecker, CharacterSetChecker>();
            services.AddScoped<IEvaluationChecker, NamingConventionChecker>();
            services.AddScoped<IEvaluationChecker, TestCaseChecker>();
            services.AddScoped<IEvaluationChecker, JUnitChecker>();

            services.AddScoped<IJavaAnalyzer, JavaAnalyzer>();

            // Liegt hier und nicht in Application: der Import braucht eine
            // Transaktion über den DbContext, die Repositories committen
            // einzeln.
            services.AddScoped<ITaskTransferService, TaskTransferService>();

            return services;
        }
    }
}
