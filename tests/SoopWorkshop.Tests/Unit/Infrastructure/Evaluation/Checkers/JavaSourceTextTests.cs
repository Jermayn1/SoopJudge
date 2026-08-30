using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class JavaSourceTextTests
    {
        [Fact]
        public void StripCommentsAndLiterals_Zeilenkommentar_WirdEntfernt()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("int wert = 1; // mein_kommentar");

            stripped.ShouldNotContain("mein_kommentar");
            stripped.ShouldContain("int wert = 1;");
        }

        [Fact]
        public void StripCommentsAndLiterals_Blockkommentar_WirdEntfernt()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("int a = 1; /* mein_kommentar */ int b = 2;");

            stripped.ShouldNotContain("mein_kommentar");
            stripped.ShouldContain("int a = 1;");
            stripped.ShouldContain("int b = 2;");
        }

        [Fact]
        public void StripCommentsAndLiterals_StringLiteral_WirdEntfernt()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("""System.out.println("mein_wert");""");

            stripped.ShouldNotContain("mein_wert");
            stripped.ShouldContain("System.out.println");
        }

        // Ein maskiertes Anführungszeichen darf das Literal nicht vorzeitig
        // beenden - sonst gilt der Rest der Zeile wieder als Code.
        [Fact]
        public void StripCommentsAndLiterals_MaskiertesAnfuehrungszeichen_BeendetDasLiteralNicht()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("""String s = "er sagte \"mein_wert\""; int b = 2;""");

            stripped.ShouldNotContain("mein_wert");
            stripped.ShouldContain("int b = 2;");
        }

        [Fact]
        public void StripCommentsAndLiterals_CharLiteral_WirdEntfernt()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("""char trenner = '_'; int wert = 1;""");

            stripped.ShouldContain("char trenner =");
            stripped.ShouldContain("int wert = 1;");
        }

        // Ein Backslash als Char-Literal ist die klassische Falle: ohne
        // Sonderbehandlung schluckt der Parser das schließende Anführungszeichen.
        [Fact]
        public void StripCommentsAndLiterals_MaskierterBackslashAlsChar_BeendetDasLiteralRichtig()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("""char c = '\\'; int wert = 1;""");

            stripped.ShouldContain("int wert = 1;");
        }

        [Fact]
        public void StripCommentsAndLiterals_Textblock_WirdEntfernt()
        {
            var source = "String s = \"\"\"\n  mein_wert\n  \"\"\"; int b = 2;";

            var stripped = JavaSourceText.StripCommentsAndLiterals(source);

            stripped.ShouldNotContain("mein_wert");
            stripped.ShouldContain("int b = 2;");
        }

        // Code darf durch das Entfernen nicht zusammenwachsen, sonst entstehen
        // Bezeichner, die es im Quelltext nie gab.
        [Fact]
        public void StripCommentsAndLiterals_Code_BleibtGetrennt()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("""mein"x"wert""");

            stripped.ShouldNotContain("meinwert");
        }

        [Fact]
        public void StripCommentsAndLiterals_ZeilenumbruecheBleibenErhalten()
        {
            var stripped = JavaSourceText.StripCommentsAndLiterals("int a = 1; // weg\nint b = 2;");

            stripped.ShouldContain("\n");
            stripped.ShouldContain("int b = 2;");
        }
    }
}
