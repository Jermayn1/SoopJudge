using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskCategoryWeightService : ITaskCategoryWeightService
    {
        private readonly ITaskCategoryWeightRepository _repository;
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly ILogger<TaskCategoryWeightService> _logger;

        public TaskCategoryWeightService(
            ITaskCategoryWeightRepository repository,
            ITaskItemRepository taskItemRepository,
            ILogger<TaskCategoryWeightService> logger)
        {
            _repository = repository;
            _taskItemRepository = taskItemRepository;
            _logger = logger;
        }

        public async Task<Result<List<TaskCategoryWeightDto>>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            var weights = await _repository.GetByTaskItemIdAsync(taskItemId);
            return Result<List<TaskCategoryWeightDto>>.Ok(weights.Select(MapToDto).ToList());
        }

        public async Task<Result<List<TaskCategoryWeightDto>>> SaveAllAsync(SaveTaskCategoryWeightsDto dto)
        {
            if (!await _taskItemRepository.ExistsAsync(dto.TaskItemId, CancellationToken.None))
                return Result<List<TaskCategoryWeightDto>>.Fail("Die angegebene Aufgabe gibt es nicht.");

            var duplicate = dto.Weights
                .GroupBy(weight => weight.Category)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                return Result<List<TaskCategoryWeightDto>>.Fail(
                    $"Für die Kategorie {duplicate.Key} sind mehrere Gewichte angegeben.");

            // Die abgeschafften Kategorien (Zeichensatz, Namenskonventionen,
            // Testfälle, Unit-Tests) nimmt der Scorer nie in die Hand. Ein Gewicht
            // darauf wäre Konfiguration, die aussieht als würde sie wirken, und
            // nichts tut - deshalb hier abgelehnt statt still gespeichert.
            var retired = dto.Weights.FirstOrDefault(
                weight => !EvaluationCategoryOrder.IsActive(weight.Category));

            if (retired is not null)
                return Result<List<TaskCategoryWeightDto>>.Fail(
                    $"Die Kategorie {retired.Category} wird nicht mehr bewertet. " +
                    $"Möglich sind: {string.Join(", ", EvaluationCategoryOrder.Active)}.");

            var invalid = dto.Weights.FirstOrDefault(weight => weight.Weight <= 0);
            if (invalid is not null)
                return Result<List<TaskCategoryWeightDto>>.Fail(
                    $"Das Gewicht für {invalid.Category} muss größer als 0 sein.");

            var weights = dto.Weights
                .Select(entry => new TaskCategoryWeight
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = dto.TaskItemId,
                    Category = entry.Category,
                    Weight = entry.Weight
                })
                .ToList();

            await _repository.ReplaceForTaskItemAsync(dto.TaskItemId, weights);

            _logger.LogInformation(
                "Gewichte von Aufgabe {TaskItemId} ersetzt: {Count} Ueberschreibung(en).",
                dto.TaskItemId, weights.Count);

            return Result<List<TaskCategoryWeightDto>>.Ok(weights.Select(MapToDto).ToList());
        }

        private static TaskCategoryWeightDto MapToDto(TaskCategoryWeight weight) => new()
        {
            Id = weight.Id,
            TaskItemId = weight.TaskItemId,
            Category = weight.Category,
            Weight = weight.Weight
        };
    }
}
