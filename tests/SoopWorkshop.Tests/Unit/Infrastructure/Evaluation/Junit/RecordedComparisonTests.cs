using SoopWorkshop.Backend.Infrastructure.Evaluation.Junit;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Junit
{
    public class RecordedComparisonTests
    {
        [Fact]
        public void TryParse_ZeileVonWerte_LiefertAlleTeile()
        {
            // "Name 1?\nAda" und "Ada" in Base64 - Zeilenumbrüche im Wert
            // dürfen die Zeile nicht zerlegen.
            var parsed = RecordedComparison.TryParse("[soop-vergleich] assertEquals 0 MainTest 7 TmFtZSAxPwpBZGE= QWRh");

            parsed.ShouldBe(new RecordedComparison("assertEquals", false, "MainTest", 7, "Name 1?\nAda", "Ada"));
        }

        // Was die Abgabe selbst nach System.err schreibt, steht im selben
        // Block. Nur was genau so aussieht wie eine Zeile von Werte, zählt.
        [Theory]
        [InlineData("Hallo von der Abgabe")]
        [InlineData("[soop-vergleich] assertEquals 1 MainTest 7 Ny41")]
        [InlineData("[soop-vergleich] assertEquals ja MainTest 7 Ny41 Ny41")]
        [InlineData("[soop-vergleich] assertEquals 1 MainTest sieben Ny41 Ny41")]
        [InlineData("[soop-vergleich] assertEquals 1 MainTest 7 kein-base64! Ny41")]
        public void TryParse_AndereZeile_LiefertNull(string line)
        {
            RecordedComparison.TryParse(line).ShouldBeNull();
        }
    }
}
