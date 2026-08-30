using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ISubmissionRepository
    {
        Task<Submission?> GetByIdAsync(Guid id);
        Task<List<Submission>> GetByTaskIdAsync(Guid taskId);

        // Seitenweise Liste für die Übersicht im Panel, neueste zuerst.
        // Lädt Aufgabe, Kategorie und Auswertung mit — aber NICHT die Dateien:
        // die Liste zeigt nur deren Anzahl, und ihr Inhalt wäre bei hunderten
        // Zeilen die teuerste Spalte der Abfrage.
        Task<(List<Submission> Items, int Total)> GetPageAsync(
            Guid? taskItemId,
            SubmissionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken);
        // Für die Ansicht einer einzelnen Abgabe im Panel: die Dateien mit
        // Inhalt, dazu Aufgabe und Kategorie für die Kopfzeile. Bewusst nicht
        // GetByIdAsync — die zieht den ganzen Aufgabengraphen mit, weil die
        // Auswertung ihn braucht.
        Task<Submission?> GetWithFilesAsync(Guid id, CancellationToken cancellationToken);

        Task AddAsync(Submission submission);
        Task UpdateAsync(Submission submission);

        // Ohne Dateien, Aufgabe und Testfälle — für Statusabfragen, die nur
        // Status und Fehlermeldung brauchen.
        Task<Submission?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken);

        // Für das Aufräumen verwaister Auswertungen beim Start.
        Task<List<Guid>> GetIdsByStatusAsync(IReadOnlyList<SubmissionStatus> statuses, CancellationToken cancellationToken);

        // Ändert gezielt nur Status und Fehlermeldung. UpdateAsync würde den
        // kompletten Graphen inklusive Dateien neu schreiben.
        Task UpdateStatusAsync(Guid id, SubmissionStatus status, string errorMessage, CancellationToken cancellationToken);
    }
}
