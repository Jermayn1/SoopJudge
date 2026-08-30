using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Services;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Application.Evaluation
{
    public class EvaluationServiceTests
    {
        private readonly ISubmissionRepository _submissionRepository = Substitute.For<ISubmissionRepository>();
        private readonly IEvaluationResultRepository _evaluationResultRepository = Substitute.For<IEvaluationResultRepository>();
        private readonly IJavaAnalyzer _javaAnalyzer = Substitute.For<IJavaAnalyzer>();

        private EvaluationService CreateService() =>
            new(_submissionRepository, _evaluationResultRepository, _javaAnalyzer, NullLogger<EvaluationService>.Instance);

        private Submission GivenSubmission()
        {
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                TaskItemId = Guid.NewGuid(),
                Task = new TaskItem { Id = Guid.NewGuid(), Tests = [] },
                Files = [new SubmissionFile { Id = Guid.NewGuid(), FileName = "Main.java", Content = "class Main {}" }]
            };

            _submissionRepository.GetByIdAsync(submission.Id).Returns(submission);
            return submission;
        }

        [Fact]
        public async Task EvaluateAsync_AuswertungErfolgreich_SpeichertErgebnisUndSetztStatusDone()
        {
            var submission = GivenSubmission();
            var evaluationResult = new EvaluationResult { Id = Guid.NewGuid(), SubmissionId = submission.Id, TotalScore = 80, MaxScore = 100 };

            _javaAnalyzer.AnalyzeAsync(submission, Arg.Any<CancellationToken>())
                .Returns(evaluationResult);

            await CreateService().EvaluateAsync(submission.Id, CancellationToken.None);

            await _evaluationResultRepository.Received(1).AddAsync(evaluationResult);
            await _submissionRepository.Received(1).UpdateStatusAsync(
                submission.Id, SubmissionStatus.Done, string.Empty, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EvaluateAsync_VorDerAuswertung_SetztStatusAufRunning()
        {
            var submission = GivenSubmission();

            _javaAnalyzer.AnalyzeAsync(submission, Arg.Any<CancellationToken>())
                .Returns(new EvaluationResult { Id = Guid.NewGuid(), SubmissionId = submission.Id });

            await CreateService().EvaluateAsync(submission.Id, CancellationToken.None);

            await _submissionRepository.Received(1).UpdateStatusAsync(
                submission.Id, SubmissionStatus.Running, string.Empty, Arg.Any<CancellationToken>());
        }

        // Früher wurde die Exception stumm geschluckt und der Grund war nirgends
        // sichtbar — das Frontend konnte den Fehlschlag gar nicht erkennen.
        [Fact]
        public async Task EvaluateAsync_AnalyzerWirft_SetztStatusFailedMitFehlermeldung()
        {
            var submission = GivenSubmission();

            _javaAnalyzer.AnalyzeAsync(submission, Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("irgendetwas ging schief"));

            await CreateService().EvaluateAsync(submission.Id, CancellationToken.None);

            await _submissionRepository.Received(1).UpdateStatusAsync(
                submission.Id,
                SubmissionStatus.Failed,
                Arg.Is<string>(message => message.Length > 0),
                Arg.Any<CancellationToken>());

            await _evaluationResultRepository.DidNotReceive().AddAsync(Arg.Any<EvaluationResult>());
        }

        // Ein Abbruch beim Herunterfahren ist kein Fehler der Abgabe: der Status
        // bleibt Running und wird beim nächsten Start aufgeräumt.
        [Fact]
        public async Task EvaluateAsync_BeimHerunterfahrenAbgebrochen_MarkiertNichtAlsFehlgeschlagen()
        {
            var submission = GivenSubmission();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            _javaAnalyzer.AnalyzeAsync(submission, Arg.Any<CancellationToken>())
                .ThrowsAsync(new OperationCanceledException());

            await Should.ThrowAsync<OperationCanceledException>(
                () => CreateService().EvaluateAsync(submission.Id, cancellation.Token));

            await _submissionRepository.DidNotReceive().UpdateStatusAsync(
                submission.Id, SubmissionStatus.Failed, Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EvaluateAsync_AbgabeExistiertNicht_TutNichts()
        {
            var submissionId = Guid.NewGuid();
            _submissionRepository.GetByIdAsync(submissionId).Returns((Submission?)null);

            await CreateService().EvaluateAsync(submissionId, CancellationToken.None);

            await _submissionRepository.DidNotReceive().UpdateStatusAsync(
                Arg.Any<Guid>(), Arg.Any<SubmissionStatus>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _javaAnalyzer.DidNotReceive().AnalyzeAsync(
                Arg.Any<Submission>(), Arg.Any<CancellationToken>());
        }
    }
}
