using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Submissions.Interfaces
{
    public interface ISubmissionService
    {
        Task<Result<SubmissionDto>> CreateAsync(
            Guid taskItemId,
            List<(string FileName, string Content)> files,
            CancellationToken cancellationToken);
        Task<Result<EvaluationResultDto>> GetResultAsync(Guid submissionId);

        // Schlägt nur fehl, wenn es die Abgabe nicht gibt — "läuft noch" und
        // "fehlgeschlagen" sind gültige Antworten, keine Fehler.
        Task<Result<SubmissionStatusDto>> GetStatusAsync(Guid submissionId, CancellationToken cancellationToken);

        // Eine einzelne Abgabe samt ihrer Dateien, für die Ansicht im Panel.
        // Schlägt nur fehl, wenn es die Abgabe nicht gibt — eine Abgabe ohne
        // Dateien wäre seltsam, aber keine Fehlermeldung wert.
        Task<Result<SubmissionDetailDto>> GetDetailAsync(Guid submissionId, CancellationToken cancellationToken);

        // Übersicht für das Panel. Eine leere Seite ist kein Fehlschlag:
        // "es gibt noch keine Abgaben" ist eine gültige Auskunft.
        Task<Result<SubmissionPageDto>> GetPageAsync(
            Guid? taskItemId,
            SubmissionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken);
    }
}