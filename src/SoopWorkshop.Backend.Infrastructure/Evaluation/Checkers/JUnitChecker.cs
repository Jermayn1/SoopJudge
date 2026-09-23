using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Junit;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Kompiliert die hinterlegten JUnit-Dateien gegen die Abgabe, führt sie über
    // den JUnit-Console-Launcher aus und liest das Ergebnis aus dem XML-Report.
    public class JUnitChecker : IEvaluationChecker
    {
        private const string ReportsDirectoryName = "junit-reports";

        private readonly IProcessRunner _processRunner;
        private readonly EvaluationOptions _options;
        private readonly ILogger<JUnitChecker> _logger;

        public JUnitChecker(
            IProcessRunner processRunner,
            IOptions<EvaluationOptions> options,
            ILogger<JUnitChecker> logger)
        {
            _processRunner = processRunner;
            _options = options.Value;
            _logger = logger;
        }

        // Dieselbe Kategorie wie die Konsolen-Testfälle: beide prüfen, ob das
        // Programm die Aufgabe erfüllt, nur auf unterschiedlichem Weg.
        public EvaluationCategory Category => EvaluationCategory.Functionality;

        public int Order => EvaluationCheckerOrder.UnitTests;

        // Der Modus entscheidet, nicht das Vorhandensein von Dateien: eine Aufgabe
        // mit vergessener Testdatei soll auffallen und nicht stillschweigend
        // milder bewertet werden.
        public bool IsApplicable(EvaluationContext context) =>
            context.Task.EvaluationMode is EvaluationMode.UnitTestOnly or EvaluationMode.Both;

        public async Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            var testFiles = context.Task.UnitTestFiles.OrderBy(file => file.Order).ToList();

            if (testFiles.Count == 0)
                throw new InvalidOperationException(
                    $"Aufgabe {context.Task.Id} ist auf {context.Task.EvaluationMode} gestellt, " +
                    "hat aber keine JUnit-Datei hinterlegt.");

            var jarPath = ResolveJarPath();
            if (!File.Exists(jarPath))
                throw new InvalidOperationException(
                    $"Das JUnit-JAR wurde unter '{jarPath}' nicht gefunden. " +
                    "Erwartet wird es unter Evaluation:JUnitJarPath.");

            // Kompiliert die Abgabe nicht, gibt es nichts auszuführen. Die
            // Kategorie fällt trotzdem nicht weg, sonst würde ihr Gewicht
            // umverteilt und kaputter Code besser bewertet.
            if (context.Compilation is null || !context.Compilation.Success)
            {
                return CheckerOutcome.WithTip(
                    "Da dein Code nicht kompiliert, konnten die Unit-Tests nicht ausgeführt werden.",
                    Failed("Die Unit-Tests konnten ausgeführt werden", string.Empty));
            }

            var rewritten = testFiles.ToDictionary(file => file, file => AssertionRewriter.Rewrite(file.Content));

            await WriteTestFilesAsync(
                context.WorkingDirectory, testFiles, file => rewritten[file].Source, cancellationToken);
            await WriteHelperAsync(context.WorkingDirectory, cancellationToken);

            var sources = testFiles.Select(file => Path.GetFileName(file.FileName)).Append(HelperPath).ToList();
            var compilation = await CompileTestFilesAsync(context.WorkingDirectory, jarPath, sources, cancellationToken);
            var callSites = CallSitesByClass(testFiles, rewritten);

            if (!compilation.Success && !compilation.TimedOut && !compilation.ExecutableNotFound)
            {
                // Mit Umleitung gescheitert. Meist liegt es an der Abgabe, dann
                // soll der Teilnehmer die Meldung zu seiner eigenen Datei sehen
                // und nicht eine zu soopjudge.Werte. Und passt die Hilfsklasse
                // einmal nicht zu einer Testdatei, darf das keine Note kosten -
                // es fehlen dann nur die Werte.
                await WriteTestFilesAsync(context.WorkingDirectory, testFiles, file => file.Content, cancellationToken);
                compilation = await CompileTestFilesAsync(
                    context.WorkingDirectory,
                    jarPath,
                    testFiles.Select(file => Path.GetFileName(file.FileName)).ToList(),
                    cancellationToken);

                if (compilation.Success)
                {
                    _logger.LogWarning(
                        "JUnit-Testdateien uebersetzen nur ohne Umleitung auf soopjudge.Werte. Die Werte bestandener Pruefungen fehlen in diesem Lauf.");
                }

                callSites = [];
            }

            if (!compilation.Success)
                return DescribeCompilationFailure(compilation);

            return await RunAsync(context.WorkingDirectory, jarPath, testFiles, callSites, cancellationToken);
        }

        // Liegt als Unterordner neben den Testdateien: soopjudge.Werte steht in
        // einem Paket, weil sich aus dem Standardpaket nichts importieren lässt.
        private static readonly string HelperPath = Path.Combine("soopjudge", "Werte.java");

        private const string HelperResource = "SoopWorkshop.Werte.java";

        private static async Task WriteHelperAsync(string workingDirectory, CancellationToken cancellationToken)
        {
            await using var stream = typeof(JUnitChecker).Assembly.GetManifestResourceStream(HelperResource)
                ?? throw new InvalidOperationException(
                    $"Die Ressource '{HelperResource}' fehlt in der Infrastructure-Assembly.");

            var target = Path.Combine(workingDirectory, HelperPath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            await using var file = File.Create(target);
            await stream.CopyToAsync(file, cancellationToken);
        }

        // Die Aufrufstellen je Testklasse. Die Klasse heißt in Java wie ihre
        // Datei, und Werte meldet den Klassennamen des Aufrufers.
        private static Dictionary<string, IReadOnlyList<AssertionRewriter.CallSite>> CallSitesByClass(
            List<TaskUnitTestFile> testFiles,
            Dictionary<TaskUnitTestFile, AssertionRewriter.Result> rewritten) =>
            testFiles.ToDictionary(
                file => Path.GetFileNameWithoutExtension(file.FileName),
                file => rewritten[file].CallSites);

        private string ResolveJarPath() =>
            Path.IsPathRooted(_options.JUnitJarPath)
                ? _options.JUnitJarPath
                : Path.Combine(AppContext.BaseDirectory, _options.JUnitJarPath);

        private static async Task WriteTestFilesAsync(
            string workingDirectory,
            List<TaskUnitTestFile> testFiles,
            Func<TaskUnitTestFile, string> content,
            CancellationToken cancellationToken)
        {
            foreach (var file in testFiles)
            {
                // Wie bei der Abgabe: nur der reine Dateiname darf ins
                // Arbeitsverzeichnis, niemals ein Pfad.
                var fileName = Path.GetFileName(file.FileName);
                await File.WriteAllTextAsync(
                    Path.Combine(workingDirectory, fileName), content(file), cancellationToken);
            }
        }

        private async Task<ProcessResult> CompileTestFilesAsync(
            string workingDirectory,
            string jarPath,
            List<string> sources,
            CancellationToken cancellationToken)
        {
            var arguments = new List<string>
            {
                "-encoding", "UTF-8",
                "-J-Dstdout.encoding=UTF-8",
                "-J-Dstderr.encoding=UTF-8",

                // Path.PathSeparator statt ';' — unter Linux trennt ':', und im
                // Betrieb läuft das hier in einem Linux-Container.
                "-cp", $"{jarPath}{Path.PathSeparator}."
            };

            arguments.AddRange(sources);

            return await _processRunner.RunAsync(
                new ProcessRequest(
                    "javac",
                    arguments,
                    workingDirectory,
                    StandardInput: null,
                    TimeSpan.FromSeconds(_options.CompileTimeoutSeconds)),
                cancellationToken);
        }

        // Die Testdatei passt nicht zur Abgabe. Das ist ein legitimes
        // Nichtbestehen — die Meldung muss aber sagen, was erwartet wurde.
        private CheckerOutcome DescribeCompilationFailure(ProcessResult compilation)
        {
            if (compilation.ExecutableNotFound)
                throw new InvalidOperationException(
                    "'javac' wurde nicht gefunden. Ohne JDK im PATH koennen Unit-Tests nicht geprueft werden.");

            if (compilation.TimedOut)
            {
                return CheckerOutcome.WithTip(
                    $"Das Kompilieren der Testdatei hat länger als {_options.CompileTimeoutSeconds} Sekunden gebraucht.",
                    Failed("Die Testdatei passt zu deiner Abgabe", string.Empty));
            }

            var rawOutput = string.IsNullOrWhiteSpace(compilation.StandardError)
                ? compilation.StandardOutput
                : compilation.StandardError;

            var explanation = JavaCompilerMessages.Translate(rawOutput);

            var tip = explanation is null
                ? "Die hinterlegten Tests lassen sich nicht gegen deine Abgabe übersetzen. " +
                  "Prüfe, ob Klassen- und Methodennamen genau wie in der Aufgabenstellung geschrieben sind."
                : explanation;

            _logger.LogInformation("JUnit-Testdatei kompiliert nicht gegen die Abgabe: {Output}", rawOutput);

            // Rohausgabe anhängen statt ersetzen: die Zeilennummer darin ist oft
            // der schnellste Weg zur Ursache.
            return CheckerOutcome.WithTip(
                tip,
                Failed("Die Testdatei passt zu deiner Abgabe", rawOutput));
        }

        private async Task<CheckerOutcome> RunAsync(
            string workingDirectory,
            string jarPath,
            List<TaskUnitTestFile> testFiles,
            Dictionary<string, IReadOnlyList<AssertionRewriter.CallSite>> callSites,
            CancellationToken cancellationToken)
        {
            var reportsDirectory = Path.Combine(workingDirectory, ReportsDirectoryName);

            var arguments = new List<string>
            {
                // Ohne diese beiden Angaben schreibt die JVM unter Windows in der
                // Codepage des Systems, auch wenn die Ausgabe umgeleitet ist.
                "-Dstdout.encoding=UTF-8",
                "-Dstderr.encoding=UTF-8",
                "-jar", jarPath,
                "execute",
                "--class-path", ".",
                "--reports-dir", ReportsDirectoryName,
                "--disable-banner",
                "--disable-ansi-colors",
                "--details=none",

                // Legt die Ausgabe auf System.err je Testfall in den Report.
                // Dort stehen die Vergleiche, die soopjudge.Werte mitschreibt.
                "--config=junit.platform.output.capture.stderr=true"
            };

            // Klassen ausdrücklich auswählen statt den Classpath zu durchsuchen:
            // in Java heißt die Datei wie die Klasse darin, das ist eindeutig.
            foreach (var file in testFiles)
            {
                arguments.Add("--select-class");
                arguments.Add(Path.GetFileNameWithoutExtension(file.FileName));
            }

            var process = await _processRunner.RunAsync(
                new ProcessRequest(
                    "java",
                    arguments,
                    workingDirectory,
                    StandardInput: null,
                    TimeSpan.FromSeconds(_options.JUnitRunTimeoutSeconds)),
                cancellationToken);

            if (process.ExecutableNotFound)
                throw new InvalidOperationException(
                    "'java' wurde nicht gefunden. Ohne JDK im PATH koennen Unit-Tests nicht ausgefuehrt werden.");

            if (process.TimedOut)
            {
                return CheckerOutcome.WithTip(
                    $"Der Testlauf hat länger als {_options.JUnitRunTimeoutSeconds} Sekunden gebraucht und wurde abgebrochen. " +
                    "Prüfe, ob eine Schleife nie endet oder auf eine Eingabe gewartet wird, die es nicht gibt.",
                    Failed("Die Unit-Tests konnten ausgeführt werden", string.Empty));
            }

            // Ein Rückgabewert ungleich 0 heißt hier nur "Tests sind
            // fehlgeschlagen" — die Wahrheit steht im Report.
            var testCases = JUnitReportReader.Read(reportsDirectory);

            if (testCases.Count == 0)
                return DescribeMissingReport(process);

            var results = testCases.Select(testCase => ToTestCaseResult(testCase, callSites)).ToArray();

            return results.All(result => result.Passed)
                ? CheckerOutcome.Of(results)
                : CheckerOutcome.WithTip(EvaluationMessages.ComparisonHint, results);
        }

        // Zwei Fälle, die sauber auseinandergehalten werden müssen:
        //
        //  - Die Meldung ließ sich zerlegen ("expected: <5> but was: <-1>").
        //    Dann füllen die beiden Werte Erwartet und Erhalten, und was davor
        //    stand, war eine eigene Meldung des Admins - die ergänzt den
        //    Anzeigenamen, statt ihn zu ersetzen.
        //  - Sie ließ sich nicht zerlegen (NullPointerException, assertTrue).
        //    Dann gehört die ganze Meldung unter "Erhalten"; im Anzeigenamen
        //    wäre ein Stacktrace-Fetzen unlesbar.
        private TestCaseResult ToTestCaseResult(
            JUnitTestCase testCase,
            Dictionary<string, IReadOnlyList<AssertionRewriter.CallSite>> callSites)
        {
            var wasSplit = testCase.Expected.Length > 0 || testCase.Actual.Length > 0;

            return new TestCaseResult
            {
                Id = Guid.NewGuid(),
                Description = wasSplit && !string.IsNullOrWhiteSpace(testCase.Message)
                    ? $"{testCase.DisplayName} ({testCase.Message})"
                    : testCase.DisplayName,
                ExpectedOutput = testCase.Expected,
                ActualOutput = wasSplit ? testCase.Actual : testCase.Message,
                Passed = testCase.Passed,
                Comparisons = ToComparisons(testCase, callSites)
            };
        }

        // Eine Schleife über hundert Werte ergäbe hundert Zeilen, die niemand
        // mehr liest. Die ersten reichen, um zu sehen, womit geprüft wurde.
        private const int MaxComparisons = 50;

        private List<TestCaseComparison> ToComparisons(
            JUnitTestCase testCase,
            Dictionary<string, IReadOnlyList<AssertionRewriter.CallSite>> callSites)
        {
            if (testCase.Comparisons.Count > MaxComparisons)
            {
                _logger.LogInformation(
                    "Testfall {Test} hat {Count} Vergleiche mitgeschrieben, gespeichert werden die ersten {Max}.",
                    testCase.DisplayName, testCase.Comparisons.Count, MaxComparisons);
            }

            return testCase.Comparisons
                .Take(MaxComparisons)
                .Select((recorded, index) => new TestCaseComparison
                {
                    Id = Guid.NewGuid(),
                    Call = FindCall(recorded, callSites) ?? string.Empty,
                    Expected = recorded.Expected,
                    Actual = recorded.Actual,
                    Passed = recorded.Passed,
                    Order = index
                })
                .ToList();
        }

        // Werte meldet Klasse und Zeile des Aufrufs. Eine innere Klasse heißt
        // für Java "KasseTest$Rechnung", ihre Datei aber KasseTest.java.
        private static string? FindCall(
            RecordedComparison recorded,
            Dictionary<string, IReadOnlyList<AssertionRewriter.CallSite>> callSites)
        {
            var className = recorded.ClassName.Split('$')[0];
            if (!callSites.TryGetValue(className, out var sites))
                return null;

            return sites.FirstOrDefault(site => site.Line == recorded.Line && site.Method == recorded.Method)?.Call;
        }

        // Kein Report trotz gelaufenem Prozess. Der häufigste Grund ist ein
        // System.exit(...) in der Abgabe: das beendet die JVM des Testlaufs und
        // reißt alle übrigen Testmethoden mit.
        private CheckerOutcome DescribeMissingReport(ProcessResult process)
        {
            _logger.LogWarning(
                "JUnit-Lauf hat keinen auswertbaren Report hinterlassen. ExitCode {ExitCode}, Ausgabe: {Output}",
                process.ExitCode,
                string.IsNullOrWhiteSpace(process.StandardError) ? process.StandardOutput : process.StandardError);

            return CheckerOutcome.WithTip(
                "Der Testlauf hat kein Ergebnis hinterlassen. Ruft dein Programm System.exit(...) auf? " +
                "Das beendet die virtuelle Maschine und bricht die Prüfung ab, bevor ein Ergebnis entsteht.",
                Failed("Die Unit-Tests konnten ausgeführt werden", process.StandardOutput));
        }

        private static TestCaseResult Failed(string description, string actualOutput) => new()
        {
            Id = Guid.NewGuid(),
            Description = description,
            ExpectedOutput = string.Empty,
            ActualOutput = actualOutput,
            Passed = false
        };
    }
}
