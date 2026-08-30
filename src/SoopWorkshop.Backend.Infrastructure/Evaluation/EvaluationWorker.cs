using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation
{
    // Arbeitet die Warteschlange ab. Ersetzt das früherer Fire-and-Forget per
    // Task.Run: die Anzahl gleichzeitiger Auswertungen ist begrenzt, und ein
    // Neustart lässt keine Abgabe mehr für immer auf "Running" stehen.
    public class EvaluationWorker : BackgroundService
    {
        private const string RestartMessage =
            "Die Auswertung wurde durch einen Neustart des Servers abgebrochen. Bitte reiche deine Lösung erneut ein.";

        private readonly IEvaluationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EvaluationOptions _options;
        private readonly ILogger<EvaluationWorker> _logger;

        public EvaluationWorker(
            IEvaluationQueue queue,
            IServiceScopeFactory scopeFactory,
            IOptions<EvaluationOptions> options,
            ILogger<EvaluationWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await FailOrphanedSubmissionsAsync(stoppingToken);

            _logger.LogInformation(
                "Auswertung bereit, {MaxConcurrency} Abgaben gleichzeitig.",
                _options.MaxConcurrency);

            await Parallel.ForEachAsync(
                _queue.ReadAllAsync(stoppingToken),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _options.MaxConcurrency,
                    CancellationToken = stoppingToken
                },
                EvaluateAsync);
        }

        private async ValueTask EvaluateAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var evaluationService = scope.ServiceProvider.GetRequiredService<IEvaluationService>();

            try
            {
                await evaluationService.EvaluateAsync(submissionId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Auswertung von {SubmissionId} beim Herunterfahren abgebrochen.", submissionId);
            }
            catch (Exception exception)
            {
                // Eine einzelne kaputte Abgabe darf den Worker nicht beenden.
                _logger.LogError(exception, "Auswertung von {SubmissionId} ist unerwartet fehlgeschlagen.", submissionId);
            }
        }

        // Abgaben aus einem früheren Prozesslauf kann niemand mehr auswerten.
        // Ohne dieses Aufräumen wartet das Frontend endlos auf ein Ergebnis.
        //
        // Fängt bewusst alles ab: eine Exception aus ExecuteAsync führt sonst dazu,
        // dass .NET den kompletten Host herunterfährt (BackgroundServiceExceptionBehavior
        // StopHost). Eine nicht erreichbare Datenbank würde dann verhindern, dass die
        // API überhaupt startet — das Aufräumen ist diesen Preis nicht wert. Bleiben
        // die verwaisten Abgaben liegen, holt der nächste Start es nach.
        private async Task FailOrphanedSubmissionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();

                var orphanedIds = await repository.GetIdsByStatusAsync(
                    [SubmissionStatus.Pending, SubmissionStatus.Running],
                    cancellationToken);

                if (orphanedIds.Count == 0)
                    return;

                foreach (var submissionId in orphanedIds)
                {
                    await repository.UpdateStatusAsync(submissionId, SubmissionStatus.Failed, RestartMessage, cancellationToken);
                }

                _logger.LogWarning(
                    "{Count} Abgabe(n) aus einem frueheren Lauf wurden als fehlgeschlagen markiert.",
                    orphanedIds.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Verwaiste Abgaben konnten beim Start nicht aufgeraeumt werden. " +
                    "Die API laeuft weiter, der naechste Start versucht es erneut.");
            }
        }
    }
}
