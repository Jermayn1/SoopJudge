using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Services;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Tests.Unit.Application.Tasks
{
    public class TaskTestServiceTests
    {
        private readonly ITaskTestRepository _repository = Substitute.For<ITaskTestRepository>();
        private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();

        private TaskTestService CreateService() =>
            new(_repository, _taskItemRepository, NullLogger<TaskTestService>.Instance);

        private Guid GivenExistingTask()
        {
            var taskItemId = Guid.NewGuid();
            _taskItemRepository.ExistsAsync(taskItemId, Arg.Any<CancellationToken>()).Returns(true);
            return taskItemId;
        }

        private static SaveTaskTestEntryDto Entry(string description, int order) => new()
        {
            Input = string.Empty,
            ExpectedOutput = "Hallo Soop",
            Description = description,
            Order = order
        };

        [Fact]
        public async Task SaveAllAsync_MehrereTestfaelle_ErsetztDenBestandInEinemZug()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskTestsDto
            {
                TaskItemId = taskItemId,
                Tests =
                [
                    Entry("Das Programm gibt den Gruss aus", 1),
                    Entry("Das Programm bricht bei leerer Eingabe nicht ab", 2)
                ]
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Count.ShouldBe(2);

            await _repository.Received(1).ReplaceForTaskItemAsync(
                taskItemId,
                Arg.Is<List<TaskTest>>(tests => tests.Count == 2));
        }

        // Eine leere Liste ist eine gültige Angabe: sie löscht alle Testfälle.
        // Das braucht der Editor, wenn der Modus auf UnitTestOnly wechselt.
        [Fact]
        public async Task SaveAllAsync_LeereListe_LoeschtAlleTestfaelle()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskTestsDto
            {
                TaskItemId = taskItemId,
                Tests = []
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.ShouldBeEmpty();

            await _repository.Received(1).ReplaceForTaskItemAsync(
                taskItemId,
                Arg.Is<List<TaskTest>>(tests => tests.Count == 0));
        }

        [Fact]
        public async Task SaveAllAsync_AufgabeExistiertNicht_LiefertFehlerUndSpeichertNichts()
        {
            _taskItemRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().SaveAllAsync(new SaveTaskTestsDto
            {
                TaskItemId = Guid.NewGuid(),
                Tests = [Entry("Das Programm gibt den Gruss aus", 1)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Aufgabe");

            await _repository.DidNotReceive().ReplaceForTaskItemAsync(
                Arg.Any<Guid>(), Arg.Any<List<TaskTest>>());
        }

        // Zwei Testfälle auf derselben Position machen die Anzeigereihenfolge
        // von der Datenbank abhängig statt von der Vorgabe.
        [Fact]
        public async Task SaveAllAsync_DoppelteReihenfolge_LehntAb()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskTestsDto
            {
                TaskItemId = taskItemId,
                Tests = [Entry("Erster Fall", 1), Entry("Zweiter Fall", 1)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Reihenfolge");

            await _repository.DidNotReceive().ReplaceForTaskItemAsync(
                Arg.Any<Guid>(), Arg.Any<List<TaskTest>>());
        }

        [Fact]
        public async Task SaveAllAsync_UnsortierteEingabe_SpeichertNachReihenfolge()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskTestsDto
            {
                TaskItemId = taskItemId,
                Tests = [Entry("Zweiter", 2), Entry("Erster", 1)]
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value![0].Description.ShouldBe("Erster");
            result.Value[1].Description.ShouldBe("Zweiter");
        }

        [Fact]
        public async Task CreateAsync_AufgabeExistiertNicht_LiefertFehler()
        {
            _taskItemRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().CreateAsync(new CreateTaskTestDto
            {
                TaskItemId = Guid.NewGuid(),
                ExpectedOutput = "Hallo Soop",
                Description = "Das Programm gibt den Gruss aus"
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Aufgabe");
            await _repository.DidNotReceive().AddAsync(Arg.Any<TaskTest>());
        }
    }
}
