using SoopWorkshop.Backend.Infrastructure.Configuration;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Configuration
{
    public class ConnectionStringSummaryTests
    {
        [Fact]
        public void Describe_VollstaendigerConnectionString_NenntServerPortUndDatenbank()
        {
            var summary = ConnectionStringSummary.Describe(
                "Host=127.0.0.1;Port=5432;Database=soopworkshop;Username=postgres;Password=geheim");

            summary.ShouldBe("127.0.0.1:5432/soopworkshop");
        }

        // Die Zusammenfassung landet im Log — das Passwort darf dort unter keinen
        // Umständen auftauchen.
        [Fact]
        public void Describe_EnthaeltNiemalsDasPasswort()
        {
            var summary = ConnectionStringSummary.Describe(
                "Host=127.0.0.1;Port=5432;Database=soopworkshop;Username=postgres;Password=streng-geheim");

            summary.ShouldNotContain("streng-geheim");
            summary.ShouldNotContain("Password", Case.Insensitive);
        }

        [Fact]
        public void Describe_OhnePortangabe_NimmtDenStandardport()
        {
            ConnectionStringSummary.Describe("Host=db;Database=soopworkshop").ShouldBe("db:5432/soopworkshop");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Describe_NichtGesetzt_SagtDasDeutlich(string? connectionString)
        {
            ConnectionStringSummary.Describe(connectionString).ShouldBe("nicht gesetzt");
        }
    }
}
