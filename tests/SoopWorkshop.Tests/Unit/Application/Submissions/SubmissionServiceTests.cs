using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Submissions.Services;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Application.Submissions
{
    public class SubmissionServiceTests
    {
        private readonly ISubmissionRepository _submissionRepository = Substitute.For<ISubmissionRepository>();
        private readonly IEvaluationResultRepository _evaluationResultRepository = Substitute.For<IEvaluationResultRepository>();
        private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
        private readonly IEvaluationQueue _evaluationQueue = Substitute.For<IEvaluationQueue>();

        private static readonly List<(string FileName, string Content)> Files =
            [("Main.java", "public class Main {}")];

        private SubmissionService CreateService() =>
            new(_submissionRepository,
                _evaluationResultRepository,
                _taskItemRepository,
                _evaluationQueue,
                NullLogger<SubmissionService>.Instance);

        private Guid GivenExistingTask()
        {
            var taskItemId = Guid.NewGuid();
            _taskItemRepository.ExistsAsync(taskItemId, Arg.Any<CancellationToken>()).Returns(true);
            return taskItemId;
        }

        [Fact]
        public async Task CreateAsync_AufgabeExistiert_SpeichertUndReihtGenauEinmalEin()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().CreateAsync(taskItemId, Files, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Value!.TaskItemId.ShouldBe(taskItemId);

            await _submissionRepository.Received(1).AddAsync(Arg.Is<Submission>(s => s.Files.Count == 1));
            await _evaluationQueue.Received(1).EnqueueAsync(result.Value.Id, Arg.Any<CancellationToken>());
        }

        // Früher schlug erst die Fremdschlüsselbedingung zu — der Teilnehmer
        // bekam einen 500er statt einer Erklärung.
        [Fact]
        public async Task CreateAsync_AufgabeExistiertNicht_LiefertFehlerUndReihtNichtsEin()
        {
            var taskItemId = Guid.NewGuid();
            _taskItemRepository.ExistsAsync(taskItemId, Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().CreateAsync(taskItemId, Files, CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Aufgabe");

            await _submissionRepository.DidNotReceive().AddAsync(Arg.Any<Submission>());
            await _evaluationQueue.DidNotReceive().EnqueueAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetStatusAsync_AbgabeFehlgeschlagen_LiefertStatusUndFehlermeldung()
        {
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                Status = SubmissionStatus.Failed,
                ErrorMessage = "Neustart des Servers",
                SubmittedAt = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc)
            };

            _submissionRepository.GetSummaryByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);

            var result = await CreateService().GetStatusAsync(submission.Id, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Status.ShouldBe(SubmissionStatus.Failed);
            result.Value.ErrorMessage.ShouldBe("Neustart des Servers");
            result.Value.SubmittedAt.ShouldBe(submission.SubmittedAt);
        }

        // "Läuft noch" ist eine gültige Antwort, kein Fehler — genau daran
        // scheiterte das Frontend beim alten /result-Endpunkt.
        [Fact]
        public async Task GetStatusAsync_AuswertungLaeuftNoch_LiefertErfolgMitStatusRunning()
        {
            var submission = new Submission { Id = Guid.NewGuid(), Status = SubmissionStatus.Running };
            _submissionRepository.GetSummaryByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);

            var result = await CreateService().GetStatusAsync(submission.Id, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Status.ShouldBe(SubmissionStatus.Running);
        }

        [Fact]
        public async Task GetStatusAsync_AbgabeUnbekannt_LiefertFehler()
        {
            var submissionId = Guid.NewGuid();
            _submissionRepository.GetSummaryByIdAsync(submissionId, Arg.Any<CancellationToken>()).Returns((Submission?)null);

            var result = await CreateService().GetStatusAsync(submissionId, CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
        }

        [Fact]
        public async Task CreateAsync_MehrereDateien_UebernimmtAlleInDieAbgabe()
        {
            var taskItemId = GivenExistingTask();
            List<(string FileName, string Content)> files =
                [("Main.java", "class Main {}"), ("Helfer.java", "class Helfer {}")];

            await CreateService().CreateAsync(taskItemId, files, CancellationToken.None);

            await _submissionRepository.Received(1).AddAsync(Arg.Is<Submission>(s =>
                s.Files.Count == 2 && s.Files.Any(f => f.FileName == "Helfer.java")));
        }

        // Die Ansicht einer einzelnen Abgabe im Panel.

        private Submission GivenAbgabeMitDateien(params (string FileName, string Content)[] dateien)
        {
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                TaskItemId = Guid.NewGuid(),
                SubmittedAt = DateTime.UtcNow,
                Status = SubmissionStatus.Done,
                Task = new TaskItem
                {
                    Title = "Bankkonto",
                    Category = new TaskCategory { Name = "OOP" }
                },
                Files = dateien
                    .Select(d => new SubmissionFile { Id = Guid.NewGuid(), FileName = d.FileName, Content = d.Content })
                    .ToList()
            };

            _submissionRepository.GetWithFilesAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);
            return submission;
        }

        [Fact]
        public async Task GetDetailAsync_LiefertDateienMitInhalt()
        {
            var submission = GivenAbgabeMitDateien(("Konto.java", "public class Konto {}"));

            var result = await CreateService().GetDetailAsync(submission.Id, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            var datei = result.Value!.Files.ShouldHaveSingleItem();
            datei.FileName.ShouldBe("Konto.java");

            // Der Inhalt ist der Grund, warum es diesen Endpunkt gibt. Ein
            // Dateiname allein hilft beim Besprechen niemandem.
            datei.Content.ShouldBe("public class Konto {}");
        }

        // SubmissionFile hat keine Order-Spalte. Ohne diese Sortierung
        // entschiede die Datenbank je Abfrage neu, und die Fenster stünden
        // bei jedem Aufruf in einer anderen Reihenfolge.
        [Fact]
        public async Task GetDetailAsync_SortiertDieDateienNachNamen()
        {
            var submission = GivenAbgabeMitDateien(
                ("Kunde.java", "class Kunde {}"),
                ("Konto.java", "class Konto {}"),
                ("Bank.java", "class Bank {}"));

            var result = await CreateService().GetDetailAsync(submission.Id, CancellationToken.None);

            result.Value!.Files.Select(f => f.FileName)
                .ShouldBe(["Bank.java", "Konto.java", "Kunde.java"]);
        }

        [Fact]
        public async Task GetDetailAsync_UebernimmtAufgabeUndKategorieFuerDieKopfzeile()
        {
            var submission = GivenAbgabeMitDateien(("Konto.java", "class Konto {}"));

            var result = await CreateService().GetDetailAsync(submission.Id, CancellationToken.None);

            result.Value!.TaskTitle.ShouldBe("Bankkonto");
            result.Value.CategoryName.ShouldBe("OOP");
            result.Value.TaskItemId.ShouldBe(submission.TaskItemId);
        }

        // Eine Abgabe ohne Dateien ist seltsam, aber keine Fehlermeldung wert -
        // die Ansicht soll die Bewertung trotzdem zeigen.
        [Fact]
        public async Task GetDetailAsync_AbgabeOhneDateien_LiefertEineLeereListe()
        {
            var submission = GivenAbgabeMitDateien();

            var result = await CreateService().GetDetailAsync(submission.Id, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Files.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDetailAsync_AbgabeUnbekannt_LiefertFehlerStattAusnahme()
        {
            var submissionId = Guid.NewGuid();
            _submissionRepository.GetWithFilesAsync(submissionId, Arg.Any<CancellationToken>())
                .Returns((Submission?)null);

            var result = await CreateService().GetDetailAsync(submissionId, CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("nicht gefunden");
        }
    }
}
