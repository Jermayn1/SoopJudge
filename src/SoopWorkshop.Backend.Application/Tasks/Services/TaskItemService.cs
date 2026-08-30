using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskItemService(
        ITaskItemRepository repository,
        ITaskCategoryRepository categoryRepository,
        ILogger<TaskItemService> logger) : ITaskItemService
    {
        private readonly ITaskItemRepository _repository = repository;
        private readonly ITaskCategoryRepository _categoryRepository = categoryRepository;
        private readonly ILogger<TaskItemService> _logger = logger;

        public async Task<Result<List<TaskItemDto>>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            var dtos = items.Select(MapToDto).ToList();
            return Result<List<TaskItemDto>>.Ok(dtos);
        }

        public async Task<Result<TaskItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<TaskItemDto>.Fail("Aufgabe nicht gefunden.");

            return Result<TaskItemDto>.Ok(MapToDto(item));
        }

        public async Task<Result<TaskItemDto>> CreateAsync(CreateTaskItemDto dto)
        {
            // Ohne diese Prüfung schlägt erst die Fremdschlüsselbedingung zu.
            // Daraus wird eine DbUpdateException, aus der die Middleware ein
            // "Ein unerwarteter Fehler ist aufgetreten." mit Status 500 macht -
            // eine Auskunft, die nicht sagt, was falsch war.
            if (!await _categoryRepository.ExistsAsync(dto.TaskCategoryId, CancellationToken.None))
                return Result<TaskItemDto>.Fail("Die angegebene Kategorie gibt es nicht.");

            var item = new TaskItem
            {
                Id = Guid.NewGuid(),
                TaskCategoryId = dto.TaskCategoryId,
                Title = dto.Title,
                Description = dto.Description,
                Difficulty = dto.Difficulty,
                Order = dto.Order,
                IsVisible = dto.IsVisible,
                EvaluationMode = dto.EvaluationMode,
                ExpectedTypes = BuildExpectedTypes(dto.ExpectedTypes),
                Hints = dto.Hints.Select((content, index) => new TaskHint
                {
                    Id = Guid.NewGuid(),
                    Content = content,
                    Order = index + 1
                }).ToList()
            };

            await _repository.AddAsync(item);

            _logger.LogInformation("Aufgabe {TaskItemId} '{Title}' angelegt.", item.Id, item.Title);

            return Result<TaskItemDto>.Ok(MapToDto(item));
        }

        public async Task<Result<TaskItemDto>> UpdateAsync(UpdateTaskItemDto dto)
        {
            var item = await _repository.GetByIdAsync(dto.Id);
            if (item is null)
                return Result<TaskItemDto>.Fail("Aufgabe nicht gefunden.");

            if (!await _categoryRepository.ExistsAsync(dto.TaskCategoryId, CancellationToken.None))
                return Result<TaskItemDto>.Fail("Die angegebene Kategorie gibt es nicht.");

            item.TaskCategoryId = dto.TaskCategoryId;
            item.Title = dto.Title;
            item.Description = dto.Description;
            item.Difficulty = dto.Difficulty;
            item.Order = dto.Order;
            item.IsVisible = dto.IsVisible;
            item.EvaluationMode = dto.EvaluationMode;

            // Ersetzen statt Abgleichen: der Vertrag kommt als Ganzes herein, und
            // was nicht mehr drinsteht, gilt nicht mehr.
            item.ExpectedTypes.Clear();
            foreach (var type in BuildExpectedTypes(dto.ExpectedTypes))
            {
                type.TaskItemId = item.Id;
                item.ExpectedTypes.Add(type);
            }


            item.Hints.Clear();
            foreach (var (content, index) in dto.Hints.Select((content, index) => (content, index)))
            {
                item.Hints.Add(new TaskHint
                {
                    // Bewusst OHNE Id: die Aufgabe ist hier bereits verfolgt, und
                    // an einem gesetzten Schlüssel erkennt die Änderungsverfolgung
                    // eine BESTEHENDE Zeile. Sie schickt dann ein UPDATE auf eine
                    // Zeile, die es nicht gibt, und wirft DbUpdateConcurrency-
                    // Exception. Ohne Id gilt der Eintrag als neu und wird eingefügt.
                    TaskItemId = item.Id,
                    Content = content,
                    Order = index + 1
                });
            }

            await _repository.UpdateAsync(item);

            _logger.LogInformation("Aufgabe {TaskItemId} geaendert.", item.Id);

            return Result<TaskItemDto>.Ok(MapToDto(item));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<bool>.Fail("Aufgabe nicht gefunden.");

            await _repository.DeleteAsync(id);

            _logger.LogInformation("Aufgabe {TaskItemId} '{Title}' geloescht.", id, item.Title);

            return Result<bool>.Ok(true);
        }

        public async Task<Result<bool>> ToggleVisibilityAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<bool>.NotFound("Aufgabe nicht gefunden.");

            if (!item.IsVisible)
            {
                // Kein NotFound: die Aufgabe gibt es, ihr fehlen nur die Daten,
                // die ihr Auswertungsmodus verlangt. Das ist eine ungültige
                // Anfrage (400), keine fehlende Ressource - sonst zeigte das
                // Frontend "gibt es nicht" für etwas, das offen vor einem liegt.
                var problem = DescribeMissingTestData(item);
                if (problem is not null)
                    return Result<bool>.Fail(problem);
            }

            item.IsVisible = !item.IsVisible;
            await _repository.UpdateAsync(item);

            _logger.LogInformation(
                "Aufgabe {TaskItemId} ist jetzt {Visibility}.",
                id,
                item.IsVisible ? "sichtbar" : "verborgen");

            return Result<bool>.Ok(item.IsVisible);
        }

        // Der geprüfte Methodenname wird aus der Signatur abgeleitet, damit der
        // Admin nur einmal aufschreiben muss, was ohnehin in der Aufgabenstellung
        // steht.
        //
        // Durchgehend ohne Id, aus demselben Grund wie bei den Tipps oben: beim
        // Ändern ist die Aufgabe verfolgt, und ein gesetzter Schlüssel lässt
        // den neuen Eintrag wie eine bestehende Zeile aussehen.
        private static List<TaskExpectedType> BuildExpectedTypes(List<ExpectedTypeInputDto> types) =>
            [.. types
                .Where(type => !string.IsNullOrWhiteSpace(type.Name))
                .Select((type, index) => new TaskExpectedType
                {
                    Name = type.Name.Trim(),
                    Order = index + 1,
                    Methods = BuildExpectedMethods(type.Methods)
                })];

        private static List<TaskExpectedMethod> BuildExpectedMethods(List<string> signatures) =>
            [.. signatures
                .Where(signature => !string.IsNullOrWhiteSpace(signature))
                .Select((signature, index) => new TaskExpectedMethod
                {
                    Signature = signature.Trim(),
                    Name = JavaSignature.ExtractMethodName(signature),
                    Order = index + 1
                })];

        // Eine sichtbare Aufgabe muss auch prüfbar sein. Geprüft wird erst beim
        // Sichtbarschalten und nicht beim Anlegen: beim Anlegen gibt es die
        // Testfälle noch gar nicht, die Aufgabe entsteht ja erst.
        //
        // Ohne diese Prüfung wird eine Aufgabe mit vergessener Testdatei still
        // milder bewertet - die Kategorie fällt weg und ihr Gewicht verteilt sich.
        private static string? DescribeMissingTestData(TaskItem item)
        {
            var needsConsoleTests = item.EvaluationMode is EvaluationMode.ConsoleOnly or EvaluationMode.Both;
            var needsUnitTests = item.EvaluationMode is EvaluationMode.UnitTestOnly or EvaluationMode.Both;

            if (needsConsoleTests && item.Tests.Count == 0)
                return $"Die Aufgabe ist auf '{item.EvaluationMode}' gestellt, hat aber keinen Konsolen-Testfall. " +
                       "Lege zuerst mindestens einen Testfall an oder stelle den Modus um.";

            if (needsUnitTests && item.UnitTestFiles.Count == 0)
                return $"Die Aufgabe ist auf '{item.EvaluationMode}' gestellt, hat aber keine JUnit-Datei. " +
                       "Hinterlege zuerst mindestens eine Testdatei oder stelle den Modus um.";

            return null;
        }

        private static TaskItemDto MapToDto(TaskItem item) => new()
        {
            Id = item.Id,
            TaskCategoryId = item.TaskCategoryId,
            Title = item.Title,
            Description = item.Description,
            Difficulty = item.Difficulty,
            Order = item.Order,
            IsVisible = item.IsVisible,
            EvaluationMode = item.EvaluationMode,
            ExpectedTypes = [.. item.ExpectedTypes
                .OrderBy(type => type.Order)
                .Select(type => new TaskExpectedTypeDto
                {
                    Id = type.Id,
                    Name = type.Name,
                    Order = type.Order,
                    Methods = [.. type.Methods
                        .OrderBy(method => method.Order)
                        .Select(method => method.Signature)]
                })],
            Hints = item.Hints
                .OrderBy(h => h.Order)
                .Select(h => new TaskHintDto
                {
                    Id = h.Id,
                    TaskItemId = h.TaskItemId,
                    Content = h.Content,
                    Order = h.Order
                }).ToList(),

            // Bewusst gefiltert: nicht freigeschaltete Testdateien verlassen den
            // Admin-Bereich nicht, sonst schreibt man auf den Test hin.
            VisibleUnitTestFiles = item.UnitTestFiles
                .Where(file => file.IsVisibleToParticipant)
                .OrderBy(file => file.Order)
                .Select(file => new TaskUnitTestFileDto
                {
                    Id = file.Id,
                    TaskItemId = file.TaskItemId,
                    FileName = file.FileName,
                    Content = file.Content,
                    Order = file.Order,
                    IsVisibleToParticipant = file.IsVisibleToParticipant
                }).ToList()
        };
    }
}
