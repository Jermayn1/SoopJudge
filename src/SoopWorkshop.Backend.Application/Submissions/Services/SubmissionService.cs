using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Submissions.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepository,
    IEvaluationResultRepository evaluationResultRepository,
    ITaskItemRepository taskItemRepository,
    IEvaluationQueue evaluationQueue,
    ILogger<SubmissionService> logger) : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository = submissionRepository;
    private readonly IEvaluationResultRepository _evaluationResultRepository = evaluationResultRepository;
    private readonly ITaskItemRepository _taskItemRepository = taskItemRepository;
    private readonly IEvaluationQueue _evaluationQueue = evaluationQueue;
    private readonly ILogger<SubmissionService> _logger = logger;

    public async Task<Result<SubmissionDto>> CreateAsync(
        Guid taskItemId,
        List<(string FileName, string Content)> files,
        CancellationToken cancellationToken)
    {
        // Ohne diese Prüfung schlägt erst die Fremdschlüsselbedingung zu und
        // der Teilnehmer bekommt einen 500er statt einer Erklärung.
        if (!await _taskItemRepository.ExistsAsync(taskItemId, cancellationToken))
            return Result<SubmissionDto>.Fail("Aufgabe nicht gefunden.");

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskItemId,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Pending,
            Files = files.Select(f => new SubmissionFile
            {
                Id = Guid.NewGuid(),
                FileName = f.FileName,
                Content = f.Content
            }).ToList()
        };

        await _submissionRepository.AddAsync(submission);

        // Die Auswertung übernimmt der EvaluationWorker. Bleibt eine Abgabe hier
        // hängen, fängt das Aufräumen beim nächsten Start sie ab.
        await _evaluationQueue.EnqueueAsync(submission.Id, cancellationToken);

        _logger.LogInformation(
            "Abgabe {SubmissionId} zu Aufgabe {TaskItemId} eingereiht ({FileCount} Datei(en)).",
            submission.Id,
            taskItemId,
            files.Count);

        return Result<SubmissionDto>.Ok(MapToDto(submission));
    }

    public async Task<Result<EvaluationResultDto>> GetResultAsync(Guid submissionId)
    {
        var submission = await _submissionRepository.GetByIdAsync(submissionId);
        if (submission is null)
            return Result<EvaluationResultDto>.Fail("Einreichung nicht gefunden.");

        if (submission.Status == SubmissionStatus.Pending || submission.Status == SubmissionStatus.Running)
            return Result<EvaluationResultDto>.Fail($"Auswertung läuft noch. Status: {submission.Status}");

        var result = await _evaluationResultRepository.GetBySubmissionIdAsync(submissionId);
        if (result is null)
            return Result<EvaluationResultDto>.Fail("Kein Ergebnis gefunden.");

        return Result<EvaluationResultDto>.Ok(MapResultToDto(result));
    }

    public async Task<Result<SubmissionStatusDto>> GetStatusAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var submission = await _submissionRepository.GetSummaryByIdAsync(submissionId, cancellationToken);
        if (submission is null)
            return Result<SubmissionStatusDto>.Fail("Einreichung nicht gefunden.");

        return Result<SubmissionStatusDto>.Ok(new SubmissionStatusDto
        {
            Id = submission.Id,
            TaskItemId = submission.TaskItemId,
            Status = submission.Status,
            SubmittedAt = submission.SubmittedAt,
            ErrorMessage = submission.ErrorMessage
        });
    }

    public async Task<Result<SubmissionDetailDto>> GetDetailAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await _submissionRepository.GetWithFilesAsync(submissionId, cancellationToken);
        if (submission is null)
            return Result<SubmissionDetailDto>.Fail("Einreichung nicht gefunden.");

        return Result<SubmissionDetailDto>.Ok(MapToDetail(submission));
    }

    private static SubmissionDetailDto MapToDetail(Submission submission) => new()
    {
        Id = submission.Id,
        TaskItemId = submission.TaskItemId,

        // Wie in der Übersicht: leerer String statt Ausnahme. Eine Kopfzeile
        // ohne Titel ist ärgerlich, eine Ansicht, die deswegen gar nicht
        // lädt, schlimmer.
        TaskTitle = submission.Task?.Title ?? string.Empty,
        CategoryName = submission.Task?.Category?.Name ?? string.Empty,

        SubmittedAt = submission.SubmittedAt,

        // Sortiert ausliefern. SubmissionFile hat keine Order-Spalte, also
        // gäbe die Datenbank die Zeilen sonst in beliebiger Reihenfolge
        // zurück und die Ansicht sähe bei jedem Aufruf anders aus.
        Files = submission.Files
            .OrderBy(f => f.FileName, StringComparer.Ordinal)
            .Select(f => new SubmissionFileDto
            {
                FileName = f.FileName,
                Content = f.Content
            }).ToList()
    };

    // Obergrenze für take. Ohne sie könnte ein Aufruf mit take=100000 die
    // ganze Tabelle samt Includes in den Speicher ziehen - eine Seitengrenze,
    // die der Aufrufer selbst bestimmt, ist keine.
    private const int MaxSeitengroesse = 200;

    public async Task<Result<SubmissionPageDto>> GetPageAsync(
        Guid? taskItemId,
        SubmissionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take <= 0 ? 25 : take, 1, MaxSeitengroesse);

        var (items, gesamt) = await _submissionRepository.GetPageAsync(
            taskItemId, status, skip, take, cancellationToken);

        _logger.LogInformation(
            "Abgaben-Uebersicht: {Anzahl} von {Gesamt} (Aufgabe {TaskItemId}, Status {Status}).",
            items.Count,
            gesamt,
            taskItemId,
            status);

        return Result<SubmissionPageDto>.Ok(new SubmissionPageDto
        {
            Items = items.Select(MapToListItem).ToList(),
            Total = gesamt,
            Skip = skip,
            Take = take
        });
    }

    private static SubmissionListItemDto MapToListItem(Submission submission) => new()
    {
        Id = submission.Id,
        TaskItemId = submission.TaskItemId,

        // Die Navigationen kommen aus dem Include der Abfrage. Der Rückfall
        // ist ein leerer String und keine Ausnahme: eine Zeile ohne Titel ist
        // ärgerlich, eine Übersicht, die deswegen gar nicht lädt, schlimmer.
        TaskTitle = submission.Task?.Title ?? string.Empty,
        CategoryName = submission.Task?.Category?.Name ?? string.Empty,

        SubmittedAt = submission.SubmittedAt,
        Status = submission.Status,
        ErrorMessage = submission.ErrorMessage,

        // Null statt 0, solange keine Auswertung vorliegt — 0 wäre eine
        // Aussage über die Lösung, null sagt nur "noch nicht bewertet".
        TotalScore = submission.EvaluationResult?.TotalScore,
        MaxScore = submission.EvaluationResult?.MaxScore
    };

    private static SubmissionDto MapToDto(Submission submission) => new()
    {
        Id = submission.Id,
        TaskItemId = submission.TaskItemId,
        SubmittedAt = submission.SubmittedAt,
        Status = submission.Status
    };

    private static EvaluationResultDto MapResultToDto(EvaluationResult result) => new()
    {
        Id = result.Id,
        SubmissionId = result.SubmissionId,
        TotalScore = result.TotalScore,
        MaxScore = result.MaxScore,
        // Sortiert ausliefern: die Datenbank gibt die Zeilen sonst in beliebiger
        // Reihenfolge zurück und die Ergebnisseite sieht bei jedem Aufruf anders aus.
        CategoryResults = result.CategoryResults
            .OrderBy(c => EvaluationCategoryOrder.Of(c.Category))
            .Select(c => new CategoryResultDto
            {
                Id = c.Id,
                Category = c.Category,
                Passed = c.Passed,
                Points = c.Points,
                MaxPoints = c.MaxPoints,
                ErrorTip = c.ErrorTip,
                TestCaseResults = c.TestCaseResults
                    .OrderBy(t => t.Order)
                    .Select(t => new TestCaseResultDto
                    {
                        Id = t.Id,
                        Description = t.Description,
                        Input = t.Input,
                        ExpectedOutput = t.ExpectedOutput,
                        ActualOutput = t.ActualOutput,
                        Passed = t.Passed,
                        Order = t.Order,
                        Comparisons = t.Comparisons
                            .OrderBy(c => c.Order)
                            .Select(c => new TestCaseComparisonDto
                            {
                                Call = c.Call,
                                Expected = c.Expected,
                                Actual = c.Actual,
                                Passed = c.Passed
                            }).ToList()
                    }).ToList()
            }).ToList()
    };
}