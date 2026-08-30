using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Tasks;
using SoopWorkshop.Backend.Application.Transfer;
using SoopWorkshop.Backend.Application.Transfer.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Transfer
{
    // Bestand heraus und wieder herein.
    //
    // Liegt bewusst in Infrastructure und benutzt den AppDbContext direkt statt
    // der Repositories: jedes Repository ruft sein eigenes SaveChangesAsync, ein
    // Import über vierzig Aufgaben könnte also mittendrin scheitern und einen
    // halben Bestand hinterlassen. Hier klammert eine Transaktion das Ganze -
    // die erste im Projekt.
    //
    // Die Entscheidungen selbst trifft dieser Dienst nicht: geprüft wird im
    // TaskBundleValidator, gerechnet im ImportPlanner. Beide sind reine
    // Funktionen in der Application-Schicht und ohne Datenbank testbar - dasselbe
    // Muster wie EvaluationScorer neben EvaluationService.
    public class TaskTransferService : ITaskTransferService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TaskTransferService> _logger;

        public TaskTransferService(AppDbContext context, ILogger<TaskTransferService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<TaskBundleDto>> ExportAsync(CancellationToken cancellationToken)
        {
            var categories = await _context.TaskCategories
                .AsNoTracking()
                .Include(category => category.Tasks).ThenInclude(task => task.Hints)
                .Include(category => category.Tasks).ThenInclude(task => task.Tests)
                .Include(category => category.Tasks).ThenInclude(task => task.UnitTestFiles)
                .Include(category => category.Tasks).ThenInclude(task => task.CategoryWeights)
                .Include(category => category.Tasks)
                    .ThenInclude(task => task.ExpectedTypes).ThenInclude(type => type.Methods)
                .OrderBy(category => category.Order).ThenBy(category => category.Id)
                .ToListAsync(cancellationToken);

            var bundle = new TaskBundleDto
            {
                FormatVersion = TaskBundleFormat.CurrentVersion,
                ExportedAt = DateTimeOffset.UtcNow,
                Categories = [.. categories.Select(ToBundle)]
            };

            _logger.LogInformation(
                "Bestand exportiert: {Categories} Kategorie(n), {Tasks} Aufgabe(n).",
                bundle.Categories.Count,
                bundle.Categories.Sum(category => category.Tasks.Count));

            return Result<TaskBundleDto>.Ok(bundle);
        }

        public async Task<Result<ImportReportDto>> PreviewAsync(
            TaskBundleDto bundle,
            ImportMode mode,
            CancellationToken cancellationToken)
        {
            var report = await BuildReportAsync(bundle, mode, cancellationToken);
            return Result<ImportReportDto>.Ok(report);
        }

        public async Task<Result<ImportReportDto>> ImportAsync(
            TaskBundleDto bundle,
            ImportMode mode,
            CancellationToken cancellationToken)
        {
            var report = await BuildReportAsync(bundle, mode, cancellationToken);

            // Solange Fehler drinstehen, wurde nichts geschrieben. Der Aufrufer
            // bekommt denselben Bericht wie bei der Vorschau.
            if (!report.IsValid)
                return Result<ImportReportDto>.Ok(report);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (mode == ImportMode.Replace)
                {
                    // Cascade räumt den kompletten Teilbaum ab - Aufgaben,
                    // Testfälle, JUnit-Dateien, Gewichte UND die Abgaben.
                    var alle = await _context.TaskCategories.ToListAsync(cancellationToken);
                    _context.TaskCategories.RemoveRange(alle);

                    // Zwischenspeichern, damit die Löschungen vor den Einfügungen
                    // liegen: sonst kollidieren die wiederverwendeten Ids.
                    await _context.SaveChangesAsync(cancellationToken);
                }

                foreach (var category in bundle.Categories)
                    await ApplyCategoryAsync(category, mode, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                // Kein stiller Fehlschlag: der Aufrufer soll erfahren, dass
                // nichts geschrieben wurde.
                _logger.LogError(ex, "Import fehlgeschlagen, es wurde nichts geschrieben.");

                return Result<ImportReportDto>.Fail(
                    "Der Import ist fehlgeschlagen, es wurde nichts geändert. " +
                    "Die Meldung steht im Protokoll des Servers.");
            }

            _logger.LogInformation(
                "Import ({Mode}) abgeschlossen: {Created} angelegt, {Updated} aktualisiert, {Deleted} geloescht.",
                mode, report.TasksCreated, report.TasksUpdated, report.TasksDeleted);

            return Result<ImportReportDto>.Ok(report);
        }

        private async Task<ImportReportDto> BuildReportAsync(
            TaskBundleDto bundle,
            ImportMode mode,
            CancellationToken cancellationToken)
        {
            var errors = TaskBundleValidator.Validate(bundle);
            if (errors.Count > 0)
                return new ImportReportDto { Errors = errors };

            // Nur die Zahlen laden, die der Planer braucht - nicht den ganzen
            // Bestand.
            var existing = await _context.TaskCategories
                .AsNoTracking()
                .Select(category => new
                {
                    category.Id,
                    Tasks = category.Tasks.Select(task => new
                    {
                        task.Id,
                        SubmissionCount = task.Submissions.Count
                    })
                })
                .ToListAsync(cancellationToken);

            var bestand = existing
                .Select(category => new ImportPlanner.ExistingCategory(
                    category.Id,
                    [.. category.Tasks.Select(task => new ImportPlanner.ExistingTask(task.Id, task.SubmissionCount))]))
                .ToList();

            return ImportPlanner.Plan(bestand, bundle, mode);
        }

        private async Task ApplyCategoryAsync(
            TaskBundleCategoryDto dto,
            ImportMode mode,
            CancellationToken cancellationToken)
        {
            // Beim Ersetzen ist alles gerade gelöscht worden - dann immer anlegen.
            var category = mode == ImportMode.Replace
                ? null
                : await _context.TaskCategories
                    .Include(c => c.Tasks)
                    .FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);

            if (category is null)
            {
                category = new TaskCategory { Id = dto.Id };
                _context.TaskCategories.Add(category);
            }

            category.Name = dto.Name;
            category.Order = dto.Order;
            category.IsVisible = dto.IsVisible;
            category.IconName = dto.IconName;

            foreach (var task in dto.Tasks)
                await ApplyTaskAsync(category, task, mode, cancellationToken);
        }

        private async Task ApplyTaskAsync(
            TaskCategory category,
            TaskBundleTaskDto dto,
            ImportMode mode,
            CancellationToken cancellationToken)
        {
            var task = mode == ImportMode.Replace
                ? null
                : await _context.TaskItems
                    .Include(t => t.Hints)
                    .Include(t => t.Tests)
                    .Include(t => t.UnitTestFiles)
                    .Include(t => t.CategoryWeights)
                    .Include(t => t.ExpectedTypes).ThenInclude(type => type.Methods)
                    .FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

            if (task is null)
            {
                task = new TaskItem { Id = dto.Id };
                _context.TaskItems.Add(task);
            }

            // Auch beim Zusammenführen: eine Aufgabe kann in der Datei in einer
            // anderen Kategorie stehen als im Bestand.
            task.Category = category;
            task.TaskCategoryId = category.Id;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Difficulty = dto.Difficulty;
            task.Order = dto.Order;
            task.IsVisible = dto.IsVisible;
            task.EvaluationMode = dto.EvaluationMode;

            // Kinder werden ersetzt, nicht abgeglichen: bei ihnen ist die Datei
            // die Wahrheit. Dasselbe Muster wie bei den SaveAll-Endpunkten.
            //
            // Durchgehend OHNE Id, wenn die Aufgabe schon verfolgt wird: an einem
            // gesetzten Schlüssel erkennt die Änderungsverfolgung eine BESTEHENDE
            // Zeile und schickt ein UPDATE auf etwas, das es nicht gibt. Das
            // trifft null Zeilen und endet in einer DbUpdateConcurrencyException.
            task.Hints.Clear();
            foreach (var (content, index) in dto.Hints.Select((content, index) => (content, index)))
                task.Hints.Add(new TaskHint { Content = content, Order = index + 1 });

            task.ExpectedTypes.Clear();
            foreach (var (type, index) in dto.ExpectedTypes.Select((type, index) => (type, index)))
            {
                var neu = new TaskExpectedType { Name = type.Name.Trim(), Order = index + 1 };

                foreach (var (signature, methodIndex) in type.Methods.Select((s, i) => (s, i)))
                {
                    neu.Methods.Add(new TaskExpectedMethod
                    {
                        Signature = signature.Trim(),
                        Name = JavaSignature.ExtractMethodName(signature),
                        Order = methodIndex + 1
                    });
                }

                task.ExpectedTypes.Add(neu);
            }

            task.Tests.Clear();
            foreach (var test in dto.Tests.OrderBy(test => test.Order))
            {
                task.Tests.Add(new TaskTest
                {
                    Input = test.Input,
                    ExpectedOutput = test.ExpectedOutput,
                    Description = test.Description,
                    Order = test.Order
                });
            }

            task.UnitTestFiles.Clear();
            foreach (var file in dto.UnitTestFiles.OrderBy(file => file.Order))
            {
                task.UnitTestFiles.Add(new TaskUnitTestFile
                {
                    FileName = file.FileName,
                    Content = file.Content,
                    Order = file.Order,
                    IsVisibleToParticipant = file.IsVisibleToParticipant
                });
            }

            task.CategoryWeights.Clear();
            foreach (var weight in dto.Weights)
            {
                task.CategoryWeights.Add(new TaskCategoryWeight
                {
                    Category = weight.Category,
                    Weight = weight.Weight
                });
            }
        }

        // Alles nach Order sortiert, damit zwei Exporte desselben Bestands
        // byte-gleich sind und sich in Git sauber diffen lassen.
        private static TaskBundleCategoryDto ToBundle(TaskCategory category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            Order = category.Order,
            IsVisible = category.IsVisible,
            IconName = category.IconName,
            Tasks = [.. category.Tasks
                .OrderBy(task => task.Order).ThenBy(task => task.Id)
                .Select(ToBundle)]
        };

        private static TaskBundleTaskDto ToBundle(TaskItem task) => new()
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Difficulty = task.Difficulty,
            Order = task.Order,
            IsVisible = task.IsVisible,
            EvaluationMode = task.EvaluationMode,
            Hints = [.. task.Hints.OrderBy(hint => hint.Order).Select(hint => hint.Content)],
            ExpectedTypes = [.. task.ExpectedTypes
                .OrderBy(type => type.Order)
                .Select(type => new TaskBundleExpectedTypeDto
                {
                    Name = type.Name,
                    Methods = [.. type.Methods.OrderBy(method => method.Order).Select(method => method.Signature)]
                })],
            Tests = [.. task.Tests
                .OrderBy(test => test.Order)
                .Select(test => new TaskBundleTestDto
                {
                    Input = test.Input,
                    ExpectedOutput = test.ExpectedOutput,
                    Description = test.Description,
                    Order = test.Order
                })],
            UnitTestFiles = [.. task.UnitTestFiles
                .OrderBy(file => file.Order)
                .Select(file => new TaskBundleUnitTestFileDto
                {
                    FileName = file.FileName,
                    Content = file.Content,
                    Order = file.Order,
                    IsVisibleToParticipant = file.IsVisibleToParticipant
                })],
            Weights = [.. task.CategoryWeights
                .OrderBy(weight => EvaluationCategoryOrder.Of(weight.Category))
                .Select(weight => new TaskBundleWeightDto
                {
                    Category = weight.Category,
                    Weight = weight.Weight
                })]
        };
    }
}
