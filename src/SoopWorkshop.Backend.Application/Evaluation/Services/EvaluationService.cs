using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Evaluation.Services
{
    // Steuert den Ablauf der Auswertung
    // 1. Läd die Submission
    // 2. Ruft den Java Analyzerauf
    // 3. speichert das Ergebnis und aktualisiert den Status
    public class EvaluationService : IEvaluationService
    {
        private const string FailureMessage =
            "Bei der Auswertung ist ein unerwarteter Fehler aufgetreten. Bitte versuche es erneut " +
            "oder melde dich beim Kursleiter, wenn es wieder passiert.";

        private readonly ISubmissionRepository _submissionRepository;
        private readonly IEvaluationResultRepository _evaluationResultRepository;
        private readonly IJavaAnalyzer _javaAnalyzer;
        private readonly ILogger<EvaluationService> _logger;

        public EvaluationService(
            ISubmissionRepository submissionRepository,
            IEvaluationResultRepository evaluationResultRepository,
            IJavaAnalyzer javaAnalyzer,
            ILogger<EvaluationService> logger)
        {
            _submissionRepository = submissionRepository;
            _evaluationResultRepository = evaluationResultRepository;
            _javaAnalyzer = javaAnalyzer;
            _logger = logger;
        }

        public async Task EvaluateAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission is null)
            {
                _logger.LogWarning("Abgabe {SubmissionId} existiert nicht mehr, Auswertung uebersprungen.", submissionId);
                return;
            }

            await _submissionRepository.UpdateStatusAsync(
                submissionId, SubmissionStatus.Running, string.Empty, cancellationToken);

            _logger.LogInformation(
                "Auswertung von {SubmissionId} gestartet ({FileCount} Datei(en)).",
                submissionId,
                submission.Files.Count);

            try
            {
                var evaluationResult = await _javaAnalyzer.AnalyzeAsync(submission, cancellationToken);

                await _evaluationResultRepository.AddAsync(evaluationResult);

                await _submissionRepository.UpdateStatusAsync(
                    submissionId, SubmissionStatus.Done, string.Empty, cancellationToken);

                _logger.LogInformation(
                    "Auswertung von {SubmissionId} abgeschlossen: {TotalScore} von {MaxScore} Punkten.",
                    submissionId,
                    evaluationResult.TotalScore,
                    evaluationResult.MaxScore);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Herunterfahren ist kein Fehler der Abgabe. Der Status bleibt auf
                // Running und wird beim nächsten Start vom Worker aufgeräumt.
                _logger.LogInformation("Auswertung von {SubmissionId} wurde abgebrochen.", submissionId);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Auswertung von {SubmissionId} ist fehlgeschlagen.", submissionId);

                await _submissionRepository.UpdateStatusAsync(
                    submissionId, SubmissionStatus.Failed, FailureMessage, CancellationToken.None);
            }
        }
    }
}
