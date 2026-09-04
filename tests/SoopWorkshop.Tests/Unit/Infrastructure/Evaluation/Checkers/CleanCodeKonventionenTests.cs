using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    // Prüft Clean Code als Ganzes statt einen einzelnen Checker, daher der Name
    // abseits vom Schema <Klasse>Tests.
    //
    // Deutschsprachiger Code schreibt Umlaute in Namen aus und setzt sie in
    // Ausgaben und Kommentaren als echte Zeichen. Beides muss die Bewertung
    // zusammen durchlassen.
    public class CleanCodeKonventionenTests
    {
        private readonly CharacterSetChecker _characterSet = new();
        private readonly NamingConventionChecker _naming = new();

        // Alle drei Teilprüfungen zusammen, so wie sie in einer Kategorie stehen.
        private async Task<IReadOnlyList<TestCaseResult>> CheckAsync(params string[] files)
        {
            var context = EvaluationContextFactory.For(files: SubmissionFileFactory.CreateMany(files));

            var characterSet = await _characterSet.CheckAsync(context, CancellationToken.None);
            var naming = await _naming.CheckAsync(context, CancellationToken.None);

            return [.. characterSet.Results, .. naming.Results];
        }

        // Deutsche Klassennamen mit ausgeschriebenem Umlaut.
        [Theory]
        [InlineData("Bruch")]
        [InlineData("Koerper")]
        [InlineData("MultiKoerper")]
        [InlineData("Wuerfel")]
        [InlineData("Kante")]
        [InlineData("Szene")]
        [InlineData("Aufgabe01")]
        [InlineData("ArrayAufsteigendSortieren")]
        [InlineData("EingabeValidierung")]
        [InlineData("Mautgebuehren")]
        [InlineData("UngueltigesJahrException")]
        [InlineData("PersoenlicheDaten")]
        public async Task CheckAsync_TypischerKlassenname_BestehtAlleTeilpruefungen(string className)
        {
            var results = await CheckAsync($"public class {className} {{ }}");

            results.ShouldAllBe(result => result.Passed);
        }

        // Dasselbe für Methodennamen.
        [Theory]
        [InlineData("kuerze")]
        [InlineData("laenge")]
        [InlineData("flaeche")]
        [InlineData("einfuegen")]
        [InlineData("summeDiagonale")]
        [InlineData("wieOftWort")]
        [InlineData("istAufsteigend")]
        [InlineData("zeileMaxSumme")]
        [InlineData("readDouble")]
        [InlineData("getUmsatz")]
        [InlineData("enthaeltZiffer")]
        [InlineData("tuermeVonHanoi")]
        public async Task CheckAsync_TypischerMethodenname_BestehtAlleTeilpruefungen(string methodName)
        {
            var results = await CheckAsync(
                $$"""
                public class Main {
                    public static void {{methodName}}() { }
                }
                """);

            results.ShouldAllBe(result => result.Passed);
        }

        // Umlaute im Kommentar und im Ausgabetext derselben Datei.
        [Fact]
        public async Task CheckAsync_UmlauteInKommentarUndAusgabe_Besteht()
        {
            var results = await CheckAsync(
                """
                import java.io.*;

                public class EingabeValidierung {

                    // Eingabekanal von der Tastatur
                    private static BufferedReader in =
                        new BufferedReader(new InputStreamReader(System.in));

                    public static double readDouble() {
                        System.out.print("Bitte double-Zahl eingeben: ");
                        return 0.0;
                    }
                }
                """);

            results.ShouldAllBe(result => result.Passed);
        }

        // Ausgabetexte, die eine Aufgabe wortgetreu vorschreiben kann.
        [Theory]
        [InlineData("Groesse in Metern: ")]
        [InlineData("Größe in Metern: ")]
        [InlineData("Der BMI beträgt ")]
        [InlineData("Kühl")]
        [InlineData("Heiß")]
        [InlineData("Menü 1")]
        [InlineData("jährlicher Zins: ")]
        [InlineData("Stromstärke [A]: ")]
        public async Task CheckAsync_VorgeschriebeneAusgabeMitUmlaut_Besteht(string output)
        {
            var results = await CheckAsync(
                $$"""
                public class Main {
                    public static void main(String[] args) {
                        System.out.println("{{output}}");
                    }
                }
                """);

            results.ShouldAllBe(result => result.Passed);
        }

        // Dieselben Wörter als Name sind ein Verstoß. Hier verläuft die Grenze.
        [Theory]
        [InlineData("public class Größe { }")]
        [InlineData("public class Körper { }")]
        [InlineData("public class Main { void berechneGröße() { } }")]
        [InlineData("public class Main { int größe = 5; }")]
        [InlineData("public class Main { void f(int höhe) { } }")]
        public async Task CheckAsync_UmlautImNamen_FaelltDurch(string code)
        {
            var results = await CheckAsync(code);

            results.ShouldContain(result => !result.Passed);
        }

        // Unterstriche im Feldnamen bleiben ein Verstoß.
        [Fact]
        public async Task CheckAsync_SnakeCaseImFeldnamen_FaelltDurch()
        {
            var results = await CheckAsync(
                """
                public class Student {
                    public int mat_nr;

                    public Student(int mat_nr) {
                        this.mat_nr = mat_nr;
                    }
                }
                """);

            results.ShouldContain(result => !result.Passed);
        }

        // Konstanten schreibt Java in Grossbuchstaben mit Unterstrich. Der
        // snake_case-Regex darf darauf nicht anschlagen.
        [Theory]
        [InlineData("public static final boolean DEMO = false;")]
        [InlineData("public static final int MAX_GROESSE = 100;")]
        [InlineData("private static final double PI_GENAU = 3.14159;")]
        public async Task CheckAsync_KonstanteInGrossbuchstaben_Besteht(string declaration)
        {
            var results = await CheckAsync(
                $$"""
                public class Main {
                    {{declaration}}
                }
                """);

            results.ShouldAllBe(result => result.Passed);
        }

        // Eine ganze Abgabe: Namen mit ausgeschriebenem Umlaut, Ausgabe und
        // Kommentar mit echtem Umlaut.
        [Fact]
        public async Task CheckAsync_TypischeMusterloesung_BestehtAlleTeilpruefungen()
        {
            var results = await CheckAsync(
                """
                import java.util.Scanner;

                public class Rechteck {

                    // Liest Laenge und Breite und gibt Flaeche und Umfang aus.
                    public static void main(String[] args) {
                        Scanner scanner = new Scanner(System.in);

                        System.out.println("Länge?");
                        double laenge = scanner.nextDouble();

                        System.out.println("Breite?");
                        double breite = scanner.nextDouble();

                        System.out.println("Fläche: " + flaeche(laenge, breite));
                        System.out.println("Umfang: " + umfang(laenge, breite));
                    }

                    private static double flaeche(double laenge, double breite) {
                        return laenge * breite;
                    }

                    private static double umfang(double laenge, double breite) {
                        return 2 * (laenge + breite);
                    }
                }
                """);

            results.Count.ShouldBe(3);
            results.ShouldAllBe(result => result.Passed);
        }

        // Mehrere Dateien in einer Abgabe.
        [Fact]
        public async Task CheckAsync_MehrereKlassen_BestehenAlleTeilpruefungen()
        {
            var results = await CheckAsync(
                """
                public class Koerper {
                    public String farbe;
                    public double flaeche() { return 0.0; }
                }
                """,
                """
                public class Kugel extends Koerper {
                    private double radius;
                    public void setRadius(double radius) { this.radius = radius; }
                }
                """);

            results.ShouldAllBe(result => result.Passed);
        }

        // Ist-Verhalten: geprüft werden genau die sieben deutschen
        // Umlautzeichen. Das Versal-Eszett und diakritische Zeichen anderer
        // Sprachen kommen durch.
        [Theory]
        [InlineData("public class Main { int GROẞE = 5; }")]
        [InlineData("public class Main { int café = 5; }")]
        [InlineData("public class Main { int año = 5; }")]
        public async Task CheckAsync_NichtDeutscherSonderbuchstabeImNamen_BestehtWeiterhin(string code)
        {
            var results = await CheckAsync(code);

            results.ShouldAllBe(result => result.Passed);
        }

        // Ist-Verhalten: Java löst die Fluchtfolge vor dem Übersetzen auf,
        // der Checker sieht dagegen nur ASCII.
        [Fact]
        public async Task CheckAsync_UmlautAlsUnicodeFluchtfolgeImNamen_BestehtWeiterhin()
        {
            var code = "public class Main { int gr" + (char)92 + "u00f6sse = 5; }";

            var results = await CheckAsync(code);

            results.ShouldAllBe(result => result.Passed);
        }

        // Ist-Verhalten: die PascalCase-Prüfung sieht nur "class".
        [Fact]
        public async Task CheckAsync_InterfaceInKleinschreibung_WirdNichtBeanstandet()
        {
            var results = await CheckAsync("public interface meinInterface { }");

            results.ShouldAllBe(result => result.Passed);
        }
    }
}
