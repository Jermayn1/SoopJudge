using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskTestService : ITaskTestService
    {
        private readonly ITaskTestRepository _repository;
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly ILogger<TaskTestService> _logger;

        public TaskTestService(
            ITaskTestRepository repository,
            ITaskItemRepository taskItemRepository,
            ILogger<TaskTestService> logger)
        {
            _repository = repository;
            _taskItemRepository = taskItemRepository;
            _logger = logger;
        }

        public async Task<Result<List<TaskTestDto>>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            var tests = await _repository.GetByTaskItemIdAsync(taskItemId);
            return Result<List<TaskTestDto>>.Ok(tests.Select(MapToDto).ToList());
        }

        public async Task<Result<TaskTestDto>> CreateAsync(CreateTaskTestDto dto)
        {
            // Sonst kommt die Fremdschlüsselbedingung als 500 zurück statt als
            // Satz, der die Ursache nennt.
            if (!await _taskItemRepository.ExistsAsync(dto.TaskItemId, CancellationToken.None))
                return Result<TaskTestDto>.Fail("Die angegebene Aufgabe gibt es nicht.");

            var test = new TaskTest
            {
                Id = Guid.NewGuid(),
                TaskItemId = dto.TaskItemId,
                Input = dto.Input,
                ExpectedOutput = dto.ExpectedOutput,
                Description = dto.Description,
                Order = dto.Order
            };

            await _repository.AddAsync(test);

            _logger.LogInformation(
                "Testfall {TaskTestId} zu Aufgabe {TaskItemId} angelegt.", test.Id, test.TaskItemId);

            return Result<TaskTestDto>.Ok(MapToDto(test));
        }

        public async Task<Result<TaskTestDto>> UpdateAsync(UpdateTaskTestDto dto)
        {
            var test = await _repository.GetByIdAsync(dto.Id);
            if (test is null)
                return Result<TaskTestDto>.Fail("Testfall nicht gefunden.");

            test.Input = dto.Input;
            test.ExpectedOutput = dto.ExpectedOutput;
            test.Description = dto.Description;
            test.Order = dto.Order;

            await _repository.UpdateAsync(test);

            _logger.LogInformation("Testfall {TaskTestId} geaendert.", test.Id);

            return Result<TaskTestDto>.Ok(MapToDto(test));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var test = await _repository.GetByIdAsync(id);
            if (test is null)
                return Result<bool>.Fail("Testfall nicht gefunden.");

            await _repository.DeleteAsync(id);

            _logger.LogInformation("Testfall {TaskTestId} geloescht.", id);

            return Result<bool>.Ok(true);
        }

        // Ersetzt alle Testfälle einer Aufgabe. Gegenstück zu
        // TaskUnitTestFileService.SaveAllAsync und nach demselben Muster gebaut.
        public async Task<Result<List<TaskTestDto>>> SaveAllAsync(SaveTaskTestsDto dto)
        {
            if (!await _taskItemRepository.ExistsAsync(dto.TaskItemId, CancellationToken.None))
                return Result<List<TaskTestDto>>.Fail("Die angegebene Aufgabe gibt es nicht.");

            // Die Reihenfolge bestimmt, in welcher Folge der Teilnehmer die
            // Teilprüfungen liest. Zwei Testfälle auf derselben Position machen
            // die Anzeige von der Datenbank abhängig, statt von der Vorgabe.
            var duplicate = dto.Tests
                .GroupBy(test => test.Order)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                return Result<List<TaskTestDto>>.Fail(
                    $"Mehrere Testfälle haben die Reihenfolge {duplicate.Key}.");

            var tests = dto.Tests
                .Select(entry => new TaskTest
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = dto.TaskItemId,
                    Input = entry.Input,
                    ExpectedOutput = entry.ExpectedOutput,
                    Description = entry.Description,
                    Order = entry.Order
                })
                .OrderBy(test => test.Order)
                .ToList();

            await _repository.ReplaceForTaskItemAsync(dto.TaskItemId, tests);

            _logger.LogInformation(
                "Testfaelle von Aufgabe {TaskItemId} ersetzt: {Count} Stueck.",
                dto.TaskItemId, tests.Count);

            return Result<List<TaskTestDto>>.Ok(tests.Select(MapToDto).ToList());
        }

        private static TaskTestDto MapToDto(TaskTest test) => new()
        {
            Id = test.Id,
            TaskItemId = test.TaskItemId,
            Input = test.Input,
            ExpectedOutput = test.ExpectedOutput,
            Description = test.Description,
            Order = test.Order
        };
    }
}
