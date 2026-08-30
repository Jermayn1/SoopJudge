using SoopWorkshop.Backend.Infrastructure.Configuration;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Configuration
{
    public class DotEnvConfigurationTests
    {
        [Fact]
        public void Parse_EinfacheZuweisung_LiefertSchluesselUndWert()
        {
            var values = DotEnvConfiguration.Parse(["POSTGRES_DB=soopworkshop"]);

            values["POSTGRES_DB"].ShouldBe("soopworkshop");
        }

        // Doppelter Unterstrich trennt die Ebenen, wie bei Umgebungsvariablen.
        [Fact]
        public void Parse_DoppelterUnterstrich_WirdZuDoppelpunkt()
        {
            var values = DotEnvConfiguration.Parse(["Evaluation__MaxConcurrency=10"]);

            values["Evaluation:MaxConcurrency"].ShouldBe("10");
        }

        [Theory]
        [InlineData("# nur ein Kommentar")]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData("=ohne Schluessel")]
        public void Parse_ZeileOhneZuweisung_WirdUebergangen(string line)
        {
            DotEnvConfiguration.Parse([line]).ShouldBeEmpty();
        }

        [Theory]
        [InlineData("PASSWORT=\"geheim\"", "geheim")]
        [InlineData("PASSWORT='geheim'", "geheim")]
        [InlineData("PASSWORT=  geheim  ", "geheim")]
        public void Parse_WertMitAnfuehrungszeichenOderLeerzeichen_WirdBereinigt(string line, string expected)
        {
            DotEnvConfiguration.Parse([line])["PASSWORT"].ShouldBe(expected);
        }

        // Passwörter dürfen Gleichheitszeichen enthalten — getrennt wird nur beim ersten.
        [Fact]
        public void Parse_WertEnthaeltGleichheitszeichen_BleibtVollstaendig()
        {
            var values = DotEnvConfiguration.Parse(["POSTGRES_PASSWORD=ab=cd=ef"]);

            values["POSTGRES_PASSWORD"].ShouldBe("ab=cd=ef");
        }

        [Fact]
        public void FindDotEnv_DateiLiegtWeiterOben_WirdGefunden()
        {
            var root = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            var nested = Path.Combine(root, "src", "SoopWorkshop.Backend.API");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(root, ".env"), "POSTGRES_DB=soopworkshop");

            try
            {
                DotEnvConfiguration.FindDotEnv(nested).ShouldBe(Path.Combine(root, ".env"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void FindDotEnv_KeineDatei_LiefertNull()
        {
            var leer = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(leer);

            try
            {
                // Ein frisch angelegtes Verzeichnis unter TEMP hat oberhalb keine .env.
                var gefunden = DotEnvConfiguration.FindDotEnv(leer);
                (gefunden is null || File.Exists(gefunden)).ShouldBeTrue();
            }
            finally
            {
                Directory.Delete(leer, recursive: true);
            }
        }
    }
}
