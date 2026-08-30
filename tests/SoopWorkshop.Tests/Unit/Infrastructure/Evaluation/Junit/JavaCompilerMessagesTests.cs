using SoopWorkshop.Backend.Infrastructure.Evaluation.Junit;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Junit
{
    public class JavaCompilerMessagesTests
    {
        // Wortlaut wie ihn javac 21 tatsächlich ausgibt - abgenommen aus einem
        // echten Lauf gegen eine Abgabe mit falsch benannter Methode.
        private const string FehlendeMethode = """
            RechnerTest.java:47: error: cannot find symbol
                    assertEquals(5, Main.addiere(2, 3));
                                        ^
              symbol:   method addiere(int,int)
              location: class Main
            2 errors
            """;

        [Fact]
        public void Translate_FehlendeMethode_NenntSignaturUndKlasse()
        {
            var explanation = JavaCompilerMessages.Translate(FehlendeMethode);

            explanation.ShouldNotBeNull();
            explanation.ShouldContain("addiere(int,int)");
            explanation.ShouldContain("Main");
            explanation.ShouldContain("Methode");
        }

        [Fact]
        public void Translate_FehlendeVariable_NenntDenBezeichner()
        {
            var explanation = JavaCompilerMessages.Translate("""
                Test.java:3: error: cannot find symbol
                  symbol:   variable zaehler
                  location: class Main
                """);

            explanation.ShouldNotBeNull();
            explanation.ShouldContain("Variable");
            explanation.ShouldContain("zaehler");
        }

        // Richtung nicht verdrehen: "A cannot be converted to B" heißt, dass B
        // erwartet wurde und A geliefert kam.
        [Fact]
        public void Translate_FalscherTyp_NenntErwartetUndGeliefert()
        {
            var explanation = JavaCompilerMessages.Translate(
                "Test.java:9: error: incompatible types: int cannot be converted to String");

            explanation.ShouldNotBeNull();
            explanation.ShouldContain("erwartet wurde „String“");
            explanation.ShouldContain("geliefert wurde „int“");
        }

        [Fact]
        public void Translate_MethodePasstNichtZuDenParametern_ErklaertDieParameter()
        {
            var explanation = JavaCompilerMessages.Translate(
                "Test.java:5: error: method addiere in class Main cannot be applied to given types;");

            explanation.ShouldNotBeNull();
            explanation.ShouldContain("addiere");
            explanation.ShouldContain("Parameter");
        }

        [Fact]
        public void Translate_KonstruktorPasstNicht_ErklaertDenKonstruktor()
        {
            var explanation = JavaCompilerMessages.Translate(
                "Test.java:5: error: constructor Konto in class Konto cannot be applied to given types;");

            explanation.ShouldNotBeNull();
            explanation.ShouldContain("Konstruktor");
        }

        [Fact]
        public void Translate_NichtOeffentlich_WeistAufPublicHin()
        {
            var explanation = JavaCompilerMessages.Translate(
                "Test.java:7: error: addiere is not public in Main; cannot be accessed from outside package");

            explanation.ShouldNotBeNull();
            explanation.ShouldContain("public");
        }

        // Lieber die Rohausgabe stehen lassen als etwas Falsches behaupten.
        [Fact]
        public void Translate_UnbekannteMeldung_LiefertNull()
        {
            JavaCompilerMessages.Translate("Test.java:1: error: irgendwas ganz anderes").ShouldBeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Translate_LeereAusgabe_LiefertNull(string output)
        {
            JavaCompilerMessages.Translate(output).ShouldBeNull();
        }
    }
}
