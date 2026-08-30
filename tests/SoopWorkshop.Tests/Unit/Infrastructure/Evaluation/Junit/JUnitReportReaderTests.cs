using SoopWorkshop.Backend.Infrastructure.Evaluation.Junit;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Junit
{
    public class JUnitReportReaderTests : IDisposable
    {
        private readonly string _directory;

        public JUnitReportReaderTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);

            GC.SuppressFinalize(this);
        }

        private void WriteReport(string fileName, string content) =>
            File.WriteAllText(Path.Combine(_directory, fileName), content);

        // Aufbau wie ihn der Console-Launcher tatsächlich schreibt, inklusive
        // der display-name-Zeile in system-out.
        private const string BestandenUndDurchgefallen = """
            <?xml version="1.0" encoding="UTF-8"?>
            <testsuite name="JUnit Jupiter" tests="2" skipped="0" failures="1" errors="0" time="0.042">
              <testcase name="faelltDurch()" classname="MainTest" time="0.006">
                <failure message="expected: &lt;etwas anderes&gt; but was: &lt;Hallo Soop&gt;" type="org.opentest4j.AssertionFailedError"><![CDATA[org.opentest4j.AssertionFailedError: expected: <etwas anderes> but was: <Hallo Soop>
            	at MainTest.faelltDurch(MainTest.java:38)
            ]]></failure>
                <system-out><![CDATA[
            unique-id: [engine:junit-jupiter]/[class:MainTest]/[method:faelltDurch()]
            display-name: JUnit Jupiter > MainTest > faellt absichtlich durch
            ]]></system-out>
              </testcase>
              <testcase name="mainGibtHalloSoopAus()" classname="MainTest" time="0.017">
                <system-out><![CDATA[
            unique-id: [engine:junit-jupiter]/[class:MainTest]/[method:mainGibtHalloSoopAus()]
            display-name: JUnit Jupiter > MainTest > main gibt Hallo Soop aus
            ]]></system-out>
              </testcase>
            </testsuite>
            """;

        [Fact]
        public void Read_ReportMitZweiTests_LiefertBeide()
        {
            WriteReport("TEST-junit-jupiter.xml", BestandenUndDurchgefallen);

            var testCases = JUnitReportReader.Read(_directory);

            testCases.Count.ShouldBe(2);
            testCases.Count(testCase => testCase.Passed).ShouldBe(1);
        }

        // Der @DisplayName ist der Text, den der Admin geschrieben hat - und der
        // einzige, der dem Teilnehmer etwas sagt.
        [Fact]
        public void Read_MitDisplayName_NutztDenLetztenAbschnitt()
        {
            WriteReport("TEST-junit-jupiter.xml", BestandenUndDurchgefallen);

            var testCases = JUnitReportReader.Read(_directory);

            testCases.Select(testCase => testCase.DisplayName)
                .ShouldBe(["faellt absichtlich durch", "main gibt Hallo Soop aus"]);
        }

        // Die Vergleichsmeldung wird zerlegt, damit ein fehlgeschlagener
        // Unit-Test genauso dargestellt werden kann wie ein Konsolen-Testfall.
        [Fact]
        public void Read_FehlgeschlagenerTest_TrenntErwartetUndErhalten()
        {
            WriteReport("TEST-junit-jupiter.xml", BestandenUndDurchgefallen);

            var failed = JUnitReportReader.Read(_directory).Single(testCase => !testCase.Passed);

            failed.Expected.ShouldBe("etwas anderes");
            failed.Actual.ShouldBe("Hallo Soop");
            failed.Message.ShouldBeEmpty();
        }

        // Was sich nicht zerlegen lässt, bleibt vollständig erhalten.
        [Fact]
        public void Read_FehlerOhneVergleich_BehaeltDieMeldung()
        {
            WriteReport("TEST-junit-jupiter.xml", """
                <testsuite name="JUnit Jupiter" tests="1">
                  <testcase name="wirft()" classname="MainTest" time="0">
                    <failure message="java.lang.NullPointerException" type="java.lang.NullPointerException" />
                  </testcase>
                </testsuite>
                """);

            var failed = JUnitReportReader.Read(_directory).ShouldHaveSingleItem();

            failed.Message.ShouldBe("java.lang.NullPointerException");
            failed.Expected.ShouldBeEmpty();
            failed.Actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_OhneDisplayName_FaelltAufKlasseUndMethodeZurueck()
        {
            WriteReport("TEST-junit-jupiter.xml", """
                <testsuite name="JUnit Jupiter" tests="1">
                  <testcase name="rechnet()" classname="RechnerTest" time="0.001" />
                </testsuite>
                """);

            JUnitReportReader.Read(_directory).ShouldHaveSingleItem()
                .DisplayName.ShouldBe("RechnerTest.rechnet()");
        }

        // Übersprungen heißt nicht bestanden: die geforderte Leistung wurde
        // nicht gezeigt.
        [Fact]
        public void Read_UebersprungenerTest_GiltAlsNichtBestanden()
        {
            WriteReport("TEST-junit-jupiter.xml", """
                <testsuite name="JUnit Jupiter" tests="1">
                  <testcase name="spaeter()" classname="MainTest" time="0">
                    <skipped message="noch nicht dran" />
                  </testcase>
                </testsuite>
                """);

            var testCase = JUnitReportReader.Read(_directory).ShouldHaveSingleItem();
            testCase.Passed.ShouldBeFalse();
            testCase.Message.ShouldBe("noch nicht dran");
        }

        [Fact]
        public void Read_FehlerStattFehlschlag_GiltAlsNichtBestanden()
        {
            WriteReport("TEST-junit-jupiter.xml", """
                <testsuite name="JUnit Jupiter" tests="1">
                  <testcase name="wirft()" classname="MainTest" time="0">
                    <error message="java.lang.NullPointerException" type="java.lang.NullPointerException" />
                  </testcase>
                </testsuite>
                """);

            var testCase = JUnitReportReader.Read(_directory).ShouldHaveSingleItem();
            testCase.Passed.ShouldBeFalse();
            testCase.Message.ShouldContain("NullPointerException");
        }

        // Der Launcher legt je Engine eine Datei an; die meisten sind leer.
        [Fact]
        public void Read_MehrereDateien_FasstSieZusammenUndUeberspringtLeere()
        {
            WriteReport("TEST-junit-jupiter.xml", BestandenUndDurchgefallen);
            WriteReport("TEST-junit-vintage.xml", """
                <testsuite name="JUnit Vintage" tests="0" />
                """);

            JUnitReportReader.Read(_directory).Count.ShouldBe(2);
        }

        // Wird die JVM mitten im Lauf beendet, bleibt eine unvollständige Datei
        // liegen. Sie darf den Leser nicht umbringen - der Aufrufer erklärt die
        // fehlenden Ergebnisse dann selbst.
        [Fact]
        public void Read_AbgeschnittenerReport_LiefertLeereListeStattZuWerfen()
        {
            WriteReport("TEST-junit-jupiter.xml", "<testsuite name=\"JUnit Jupiter\"><testcase name=\"a(");

            JUnitReportReader.Read(_directory).ShouldBeEmpty();
        }

        [Fact]
        public void Read_VerzeichnisFehlt_LiefertLeereListe()
        {
            JUnitReportReader.Read(Path.Combine(_directory, "gibtesnicht")).ShouldBeEmpty();
        }
    }
}
