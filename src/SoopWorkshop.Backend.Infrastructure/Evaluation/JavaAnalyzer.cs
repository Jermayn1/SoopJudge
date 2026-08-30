using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Application.Evaluation.Scoring;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation
{
    // Führt die registrierten Checker aus und lässt den EvaluationScorer daraus
    // die Punkte berechnen. Eine neue Prüfung wird nur noch in der DI angemeldet -
    // hier ist dafür keine Änderung mehr nötig.
    public class JavaAnalyzer : IJavaAnalyzer
    {
        private readonly IReadOnlyList<IEvaluationChecker> _checkers;
        private readonly EvaluationOptions _options;
        private readonly ILogger<JavaAnalyzer> _logger;

        public JavaAnalyzer(
            IEnumerable<IEvaluationChecker> checkers,
            IOptions<EvaluationOptions> options,
            ILogger<JavaAnalyzer> logger)
        {
            // Einmal sortieren statt bei jeder Abgabe: die Reihenfolge ist
            // verbindlich, weil spätere Checker das Kompilierergebnis brauchen.
            _checkers = [.. checkers.OrderBy(checker => checker.Order)];
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EvaluationResult> AnalyzeAsync(Submission submission, CancellationToken cancellationToken)
        {
            // Das Arbeitsverzeichnis gehört dem Analyzer, damit das finally unten es
            // auch dann löscht, wenn ein Checker eine Exception wirft.
            var workingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop", Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDirectory);

            try
            {
                var context = new EvaluationContext
                {
                    Submission = submission,
                    Task = submission.Task,
                    WorkingDirectory = workingDirectory,
                    Files = [.. submission.Files]
                };

                var scoreInputs = await RunCheckersAsync(context, cancellationToken);
                var categoryResults = EvaluationScorer.Score(scoreInputs);

                var evaluationResult = new EvaluationResult
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    TotalScore = categoryResults.Sum(category => category.Points),
                    MaxScore = categoryResults.Sum(category => category.MaxPoints),
                    CategoryResults = [.. categoryResults]
                };

                foreach (var categoryResult in categoryResults)
                {
                    categoryResult.EvaluationResultId = evaluationResult.Id;
                }

                return evaluationResult;
            }
            finally
            {
                CleanupWorkingDirectory(workingDirectory);
            }
        }

        // Sammelt die Teilprüfungen der anwendbaren Checker je Kategorie ein.
        // Mehrere Checker dürfen auf dieselbe Kategorie einzahlen - Clean Code
        // besteht genau so aus Zeichensatz- und Namensprüfung.
        private async Task<List<CategoryScoreInput>> RunCheckersAsync(
            EvaluationContext context,
            CancellationToken cancellationToken)
        {
            var results = new Dictionary<EvaluationCategory, List<TestCaseResult>>();
            var errorTips = new Dictionary<EvaluationCategory, List<string>>();

            foreach (var checker in _checkers)
            {
                if (!checker.IsApplicable(context))
                {
                    _logger.LogDebug(
                        "Checker {Checker} ist fuer Aufgabe {TaskItemId} nicht anwendbar und wird uebersprungen.",
                        checker.GetType().Name,
                        context.Task.Id);
                    continue;
                }

                var outcome = await checker.CheckAsync(context, cancellationToken);

                if (!results.TryGetValue(checker.Category, out var categoryResults))
                {
                    categoryResults = [];
                    results[checker.Category] = categoryResults;
                    errorTips[checker.Category] = [];
                }

                categoryResults.AddRange(outcome.Results);

                // Doppelte Hinweise verwerfen: zahlen zwei Checker auf dieselbe
                // Kategorie ein, sagen sie im Regelfall dasselbe. Aneinandergereiht
                // liest der Teilnehmer sonst einen Absatz statt eines Satzes.
                if (!string.IsNullOrWhiteSpace(outcome.ErrorTip)
                    && !errorTips[checker.Category].Contains(outcome.ErrorTip))
                {
                    errorTips[checker.Category].Add(outcome.ErrorTip);
                }
            }

            return [.. results.Select(entry => new CategoryScoreInput(
                entry.Key,
                ResolveWeight(context.Task, entry.Key),
                entry.Value,
                // Mehrere Hinweise einer Kategorie hintereinander, damit keiner verloren geht.
                errorTips[entry.Key].Count == 0 ? null : string.Join(" ", errorTips[entry.Key])))];
        }

        // Aufgabenspezifisches Gewicht schlägt den Standard aus der Konfiguration.
        private double ResolveWeight(TaskItem task, EvaluationCategory category)
        {
            var taskWeight = task.CategoryWeights.FirstOrDefault(weight => weight.Category == category);
            if (taskWeight is not null)
                return taskWeight.Weight;

            if (_options.CategoryWeights.TryGetValue(category, out var configuredWeight))
                return configuredWeight;

            throw new InvalidOperationException(
                $"Fuer die Kategorie {category} ist kein Gewicht hinterlegt. " +
                "Erwartet wird ein Eintrag unter Evaluation:CategoryWeights.");
        }

        // Löscht das temporäre Verzeichnis mit dem kompilierten Dateien damit der Speicher nicht unnötig voll wird
        private void CleanupWorkingDirectory(string workingDirectory)
        {
            if (!Directory.Exists(workingDirectory))
                return;

            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Aufräumfehler sollen die Auswertung nicht fehlschlagen lassen — stumm
                // bleiben dürfen sie trotzdem nicht, sonst läuft die Platte unbemerkt voll.
                _logger.LogWarning(exception, "Arbeitsverzeichnis {WorkingDirectory} konnte nicht geloescht werden.", workingDirectory);
            }
        }
    }
}
