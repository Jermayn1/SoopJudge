using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskUnitTestFileService : ITaskUnitTestFileService
    {
        private const string JavaExtension = ".java";

        private readonly ITaskUnitTestFileRepository _repository;
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly ILogger<TaskUnitTestFileService> _logger;

        public TaskUnitTestFileService(
            ITaskUnitTestFileRepository repository,
            ITaskItemRepository taskItemRepository,
            ILogger<TaskUnitTestFileService> logger)
        {
            _repository = repository;
            _taskItemRepository = taskItemRepository;
            _logger = logger;
        }

        public async Task<Result<List<TaskUnitTestFileDto>>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            var files = await _repository.GetByTaskItemIdAsync(taskItemId);
            return Result<List<TaskUnitTestFileDto>>.Ok(files.Select(MapToDto).ToList());
        }

        public async Task<Result<TaskUnitTestFileDto>> CreateAsync(CreateTaskUnitTestFileDto dto)
        {
            var error = ValidateFileName(dto.FileName);
            if (error is not null)
                return Result<TaskUnitTestFileDto>.Fail(error);

            // Sonst kommt die Fremdschlüsselbedingung als 500 zurück.
            if (!await _taskItemRepository.ExistsAsync(dto.TaskItemId, CancellationToken.None))
                return Result<TaskUnitTestFileDto>.Fail("Die angegebene Aufgabe gibt es nicht.");

            var file = new TaskUnitTestFile
            {
                Id = Guid.NewGuid(),
                TaskItemId = dto.TaskItemId,
                FileName = dto.FileName,
                Content = dto.Content,
                Order = dto.Order,
                IsVisibleToParticipant = dto.IsVisibleToParticipant
            };

            await _repository.AddAsync(file);

            _logger.LogInformation(
                "JUnit-Datei {FileId} '{FileName}' zu Aufgabe {TaskItemId} angelegt.",
                file.Id, file.FileName, file.TaskItemId);

            return Result<TaskUnitTestFileDto>.Ok(MapToDto(file));
        }

        public async Task<Result<TaskUnitTestFileDto>> UpdateAsync(UpdateTaskUnitTestFileDto dto)
        {
            var error = ValidateFileName(dto.FileName);
            if (error is not null)
                return Result<TaskUnitTestFileDto>.Fail(error);

            var file = await _repository.GetByIdAsync(dto.Id);
            if (file is null)
                return Result<TaskUnitTestFileDto>.Fail("JUnit-Datei nicht gefunden.");

            file.FileName = dto.FileName;
            file.Content = dto.Content;
            file.Order = dto.Order;
            file.IsVisibleToParticipant = dto.IsVisibleToParticipant;

            await _repository.UpdateAsync(file);

            _logger.LogInformation("JUnit-Datei {FileId} geaendert.", file.Id);

            return Result<TaskUnitTestFileDto>.Ok(MapToDto(file));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var file = await _repository.GetByIdAsync(id);
            if (file is null)
                return Result<bool>.Fail("JUnit-Datei nicht gefunden.");

            await _repository.DeleteAsync(id);

            _logger.LogInformation("JUnit-Datei {FileId} geloescht.", id);

            return Result<bool>.Ok(true);
        }

        public async Task<Result<List<TaskUnitTestFileDto>>> SaveAllAsync(SaveTaskUnitTestFilesDto dto)
        {
            foreach (var entry in dto.Files)
            {
                var error = ValidateFileName(entry.FileName);
                if (error is not null)
                    return Result<List<TaskUnitTestFileDto>>.Fail(error);
            }

            if (!await _taskItemRepository.ExistsAsync(dto.TaskItemId, CancellationToken.None))
                return Result<List<TaskUnitTestFileDto>>.Fail("Die angegebene Aufgabe gibt es nicht.");

            var duplicate = dto.Files
                .GroupBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                return Result<List<TaskUnitTestFileDto>>.Fail(
                    $"Der Dateiname „{duplicate.Key}“ kommt mehrfach vor. Im selben Arbeitsverzeichnis " +
                    "würde die eine Datei die andere überschreiben.");

            var files = dto.Files
                .Select(entry => new TaskUnitTestFile
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = dto.TaskItemId,
                    FileName = entry.FileName,
                    Content = entry.Content,
                    Order = entry.Order,
                    IsVisibleToParticipant = entry.IsVisibleToParticipant
                })
                .ToList();

            await _repository.ReplaceForTaskItemAsync(dto.TaskItemId, files);

            _logger.LogInformation(
                "JUnit-Dateien von Aufgabe {TaskItemId} ersetzt: jetzt {Count} Datei(en).",
                dto.TaskItemId, files.Count);

            return Result<List<TaskUnitTestFileDto>>.Ok(files.Select(MapToDto).ToList());
        }

        // Der Dateiname landet als echte Datei im Arbeitsverzeichnis und muss in
        // Java zum Klassennamen passen - deshalb hier prüfen und nicht erst,
        // wenn javac beim Teilnehmer scheitert.
        private static string? ValidateFileName(string fileName)
        {
            if (!fileName.EndsWith(JavaExtension, StringComparison.OrdinalIgnoreCase))
                return $"Der Dateiname '{fileName}' muss auf {JavaExtension} enden.";

            if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
                return $"Der Dateiname '{fileName}' darf keine Pfadangaben enthalten.";

            return null;
        }

        private static TaskUnitTestFileDto MapToDto(TaskUnitTestFile file) => new()
        {
            Id = file.Id,
            TaskItemId = file.TaskItemId,
            FileName = file.FileName,
            Content = file.Content,
            Order = file.Order,
            IsVisibleToParticipant = file.IsVisibleToParticipant
        };
    }
}
