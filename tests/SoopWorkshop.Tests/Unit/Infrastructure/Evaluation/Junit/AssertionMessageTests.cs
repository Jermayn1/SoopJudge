using SoopWorkshop.Backend.Infrastructure.Evaluation.Junit;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Junit
{
    public class AssertionMessageTests
    {
        // Die Standardform von opentest4j - so meldet assertEquals.
        [Fact]
        public void Split_VergleichsMeldung_TrenntErwartetUndErhalten()
        {
            var parts = AssertionMessage.Split("expected: <5> but was: <-1>");

            parts.Expected.ShouldBe("5");
            parts.Actual.ShouldBe("-1");
            parts.Message.ShouldBeEmpty();
        }

        // Schreibt der Admin eine eigene Meldung, steht sie mit "==>" davor.
        // Sie darf nicht verloren gehen - sie sagt oft, worauf es ankam.
        [Fact]
        public void Split_MitEigenerMeldung_BehaeltSieAlsMeldung()
        {
            var parts = AssertionMessage.Split("Die Summe stimmt nicht ==> expected: <5> but was: <-1>");

            parts.Message.ShouldBe("Die Summe stimmt nicht");
            parts.Expected.ShouldBe("5");
            parts.Actual.ShouldBe("-1");
        }

        [Fact]
        public void Split_MehrzeiligeWerte_WerdenVollstaendigUebernommen()
        {
            var parts = AssertionMessage.Split("expected: <Zeile 1\nZeile 2> but was: <Zeile 1>");

            parts.Expected.ShouldBe("Zeile 1\nZeile 2");
            parts.Actual.ShouldBe("Zeile 1");
        }

        [Fact]
        public void Split_LeereWerte_WerdenUebernommen()
        {
            var parts = AssertionMessage.Split("expected: <Hallo> but was: <>");

            parts.Expected.ShouldBe("Hallo");
            parts.Actual.ShouldBeEmpty();
        }

        // Nicht jede Meldung ist ein Vergleich. Lieber roh stehen lassen als
        // etwas Falsches herauslesen.
        [Theory]
        [InlineData("java.lang.NullPointerException: Cannot invoke \"String.length()\"")]
        [InlineData("Ein Wert war erwartet, aber nichts kam an")]
        public void Split_KeinVergleich_LaesstDieMeldungUnveraendert(string message)
        {
            var parts = AssertionMessage.Split(message);

            parts.Message.ShouldBe(message);
            parts.Expected.ShouldBeEmpty();
            parts.Actual.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Split_OhneMeldung_LiefertLeereTeile(string? message)
        {
            var parts = AssertionMessage.Split(message);

            parts.Message.ShouldBeEmpty();
            parts.Expected.ShouldBeEmpty();
            parts.Actual.ShouldBeEmpty();
        }
    }
}
