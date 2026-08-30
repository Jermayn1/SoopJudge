using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SoopWorkshop.Backend.Infrastructure.Configuration;

namespace SoopWorkshop.Backend.Infrastructure.Persistence
{
    // Wird ausschließlich von den EF-Core-Tools benutzt (dotnet ef migrations / database update).
    // Liest denselben Connection-String wie Backend.API: appsettings.json, User Secrets,
    // Umgebungsvariablen, zuletzt die .env - spätere gewinnen.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        // Muss mit der UserSecretsId in SoopWorkshop.Backend.API.csproj übereinstimmen
        private const string UserSecretsId = "soopworkshop-backend-api";

        public AppDbContext CreateDbContext(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var apiProjectPath = Path.Combine(currentDirectory, "..", "SoopWorkshop.Backend.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets(UserSecretsId)
                .AddEnvironmentVariables()
                .AddDotEnv(currentDirectory)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Es ist kein Connection-String gesetzt. Lokal die .env im Repository-Wurzelverzeichnis" + Environment.NewLine +
                    "anlegen (Vorlage: .env.example) und POSTGRES_PASSWORD setzen.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
