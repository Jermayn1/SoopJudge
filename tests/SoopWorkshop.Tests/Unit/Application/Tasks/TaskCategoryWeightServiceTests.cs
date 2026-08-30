using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Services;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Application.Tasks
{
    public class TaskCategoryWeightServiceTests
    {
        private readonly ITaskCategoryWeightRepository _repository =
            Substitute.For<ITaskCategoryWeightRepository>();

        private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();

        private TaskCategoryWeightService CreateService() =>
            new(_repository, _taskItemRepository, NullLogger<TaskCategoryWeightService>.Instance);

        private Guid GivenExistingTask()
        {
            var taskItemId = Guid.NewGuid();
            _taskItemRepository.ExistsAsync(taskItemId, Arg.Any<CancellationToken>()).Returns(true);
            return taskItemId;
        }

        private static SaveTaskCategoryWeightEntryDto Entry(EvaluationCategory category, double weight) =>
            new() { Category = category, Weight = weight };

        [Fact]
        public async Task SaveAllAsync_AktiveKategorien_Speichert()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskCategoryWeightsDto
            {
                TaskItemId = taskItemId,
                Weights =
                [
                    Entry(EvaluationCategory.CleanCode, 15),
                    Entry(EvaluationCategory.Compilability, 20),
                    Entry(EvaluationCategory.Functionality, 65)
                ]
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Count.ShouldBe(3);

            await _repository.Received(1).ReplaceForTaskItemAsync(
                taskItemId,
                Arg.Is<List<TaskCategoryWeight>>(weights => weights.Count == 3));
        }

        // Die abgeschafften Kategorien liest der Scorer nie. Ein Gewicht darauf
        // wäre Konfiguration, die aussieht als würde sie wirken, und nichts tut.
        [Theory]
        [InlineData(EvaluationCategory.CharacterSet)]
        [InlineData(EvaluationCategory.NamingConventions)]
        [InlineData(EvaluationCategory.TestCases)]
        [InlineData(EvaluationCategory.UnitTests)]
        public async Task SaveAllAsync_AbgeschaffteKategorie_LehntAb(EvaluationCategory retired)
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskCategoryWeightsDto
            {
                TaskItemId = taskItemId,
                Weights = [Entry(retired, 10)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("nicht mehr bewertet");

            await _repository.DidNotReceive().ReplaceForTaskItemAsync(
                Arg.Any<Guid>(), Arg.Any<List<TaskCategoryWeight>>());
        }

        [Fact]
        public async Task SaveAllAsync_AufgabeExistiertNicht_LiefertFehler()
        {
            _taskItemRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().SaveAllAsync(new SaveTaskCategoryWeightsDto
            {
                TaskItemId = Guid.NewGuid(),
                Weights = [Entry(EvaluationCategory.CleanCode, 15)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Aufgabe");
        }

        [Fact]
        public async Task SaveAllAsync_ZweiGewichteFuerDieselbeKategorie_LehntAb()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskCategoryWeightsDto
            {
                TaskItemId = taskItemId,
                Weights =
                [
                    Entry(EvaluationCategory.CleanCode, 15),
                    Entry(EvaluationCategory.CleanCode, 30)
                ]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("mehrere Gewichte");
        }

        // Ein Gewicht von 0 würde die Kategorie aus der Normierung nehmen, ohne
        // dass jemand das so gemeint hat — der Scorer wirft darauf.
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task SaveAllAsync_GewichtNichtPositiv_LehntAb(double weight)
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskCategoryWeightsDto
            {
                TaskItemId = taskItemId,
                Weights = [Entry(EvaluationCategory.CleanCode, weight)]
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("größer als 0");
        }

        // Leere Liste heißt "Standardgewichte aus der Konfiguration gelten wieder".
        [Fact]
        public async Task SaveAllAsync_LeereListe_StelltStandardgewichteWiederHer()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().SaveAllAsync(new SaveTaskCategoryWeightsDto
            {
                TaskItemId = taskItemId,
                Weights = []
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.ShouldBeEmpty();

            await _repository.Received(1).ReplaceForTaskItemAsync(
                taskItemId,
                Arg.Is<List<TaskCategoryWeight>>(weights => weights.Count == 0));
        }
    }
}
