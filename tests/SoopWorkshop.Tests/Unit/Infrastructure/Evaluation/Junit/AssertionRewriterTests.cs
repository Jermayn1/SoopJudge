using SoopWorkshop.Backend.Infrastructure.Evaluation.Junit;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Junit
{
    public class AssertionRewriterTests
    {
        [Fact]
        public void Rewrite_UnqualifizierterAufruf_LeitetAufWerteUm()
        {
            var result = AssertionRewriter.Rewrite("assertEquals(7, Rechner.addiere(3, 4));");

            result.Source.ShouldBe("soopjudge.Werte.assertEquals(7, Rechner.addiere(3, 4));");
        }

        [Theory]
        [InlineData("Assertions.assertTrue(x.ok());")]
        [InlineData("org.junit.jupiter.api.Assertions.assertTrue(x.ok());")]
        public void Rewrite_QualifizierterAufruf_ErsetztDenQualifier(string source)
        {
            AssertionRewriter.Rewrite(source).Source.ShouldBe("soopjudge.Werte.assertTrue(x.ok());");
        }

        // Die Zuordnung hängt an der Zeilennummer, die Werte meldet. Verschiebt
        // die Umschreibung eine Zeile, landet ein Wert beim falschen Aufruf.
        [Fact]
        public void Rewrite_BehaeltDieZeilen()
        {
            const string source = "class T {\n  void a() {\n    assertEquals(1, f());\n  }\n}\n";

            var result = AssertionRewriter.Rewrite(source);

            result.Source.Split('\n').Length.ShouldBe(source.Split('\n').Length);
            result.CallSites.ShouldHaveSingleItem().Line.ShouldBe(3);
        }

        [Fact]
        public void Rewrite_Import_BleibtUnangetastet()
        {
            const string source = "import static org.junit.jupiter.api.Assertions.assertEquals;";

            AssertionRewriter.Rewrite(source).Source.ShouldBe(source);
        }

        [Theory]
        [InlineData("String s = \"assertEquals(1, 2)\";")]
        [InlineData("// assertEquals(1, 2)")]
        [InlineData("/* assertTrue(x()) */")]
        [InlineData("kasse.assertEquals(1, 2);")]
        [InlineData("MeineAssertions.assertEquals(1, 2);")]
        public void Rewrite_KeinPruefaufruf_BleibtUnangetastet(string source)
        {
            var result = AssertionRewriter.Rewrite(source);

            result.Source.ShouldBe(source);
            result.CallSites.ShouldBeEmpty();
        }

        // Deklariert die Testdatei selbst eine Methode assertEquals, meint jeder
        // Aufruf darin diese Methode. Umgeleitet riefe er etwas anderes auf.
        [Fact]
        public void Rewrite_EigeneMethodeGleichenNamens_BleibtUnangetastet()
        {
            const string source = """
                class T {
                  void assertEquals(String a, String b) { }
                  void t() { assertEquals("x", "y"); assertTrue(f()); }
                }
                """;

            var result = AssertionRewriter.Rewrite(source);

            result.Source.ShouldContain("void assertEquals(String a, String b)");
            result.Source.ShouldContain(" assertEquals(\"x\", \"y\")");
            result.Source.ShouldContain("soopjudge.Werte.assertTrue(f())");
        }

        [Fact]
        public void Rewrite_NachReturn_IstEinAufruf()
        {
            var result = AssertionRewriter.Rewrite("return assertThrows(IllegalStateException.class, () -> k.boom());");

            result.Source.ShouldStartWith("return soopjudge.Werte.assertThrows(");
        }

        [Fact]
        public void Rewrite_MehrzeiligerAufruf_MeldetDieStartzeileUndDenAusdruck()
        {
            const string source = "\nassertFalse(Pangram.istPangram(\n    \"abc\"));";

            var site = AssertionRewriter.Rewrite(source).CallSites.ShouldHaveSingleItem();

            site.Line.ShouldBe(2);
            site.Method.ShouldBe("assertFalse");
            site.Call.ShouldBe("Pangram.istPangram(\"abc\")");
        }

        [Theory]
        [InlineData("assertEquals(1, Ggt.ggt(9, 28));", "Ggt.ggt(9, 28)")]
        [InlineData("assertEquals(7.5, k.bezahlen(20), 0.0001, \"Wechselgeld, bitte\");", "k.bezahlen(20)")]
        [InlineData("assertArrayEquals(new int[] {1, 2}, Feld.drehe(new int[] {2, 1}));", "Feld.drehe(new int[] {2, 1})")]
        [InlineData("assertTrue(IstPrim.istPrim(7));", "IstPrim.istPrim(7)")]
        [InlineData("assertThrows(IllegalStateException.class, () -> k.boom());", "k.boom()")]
        [InlineData("assertThrows(IllegalStateException.class, () -> { k.boom(); });", "k.boom()")]
        public void Rewrite_LiefertDenGeprueftenAusdruck(string source, string expected)
        {
            AssertionRewriter.Rewrite(source).CallSites.ShouldHaveSingleItem().Call.ShouldBe(expected);
        }

        // Bei einer Variablen oder einem Literal gibt es nichts zu erfahren,
        // was nicht schon unter Erhalten steht.
        [Theory]
        [InlineData("assertEquals(\"Hallo\", ausgabe);")]
        [InlineData("assertTrue(true);")]
        public void Rewrite_OhneAufruf_LiefertKeinenAusdruck(string source)
        {
            AssertionRewriter.Rewrite(source).CallSites.ShouldHaveSingleItem().Call.ShouldBeNull();
        }
    }
}
