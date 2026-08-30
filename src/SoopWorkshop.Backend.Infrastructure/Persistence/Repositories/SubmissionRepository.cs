using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly AppDbContext _context;

        public SubmissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Submission?> GetByIdAsync(Guid id)
        {
            // Die Auswertung liest alles über submission.Task - was hier fehlt,
            // sieht der JavaAnalyzer als "nicht vorhanden" und bewertet entsprechend.
            return await _context.Submissions
                .Include(s => s.Files)
                .Include(s => s.Task)
                    .ThenInclude(t => t.Tests)
                .Include(s => s.Task)
                    .ThenInclude(t => t.CategoryWeights)
                .Include(s => s.Task)
                    .ThenInclude(t => t.UnitTestFiles)
                .Include(s => s.Task)
                    .ThenInclude(t => t.ExpectedTypes)
                        .ThenInclude(type => type.Methods)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Submission>> GetByTaskIdAsync(Guid taskId)
        {
            return await _context.Submissions
                .Where(s => s.TaskItemId == taskId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task<(List<Submission> Items, int Total)> GetPageAsync(
            Guid? taskItemId,
            SubmissionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var abfrage = _context.Submissions.AsNoTracking().AsQueryable();

            if (taskItemId is not null)
                abfrage = abfrage.Where(s => s.TaskItemId == taskItemId);

            if (status is not null)
                abfrage = abfrage.Where(s => s.Status == status);

            // Vor dem Blättern zählen, sonst zählt man die Seite statt der
            // Menge - und die Seitennavigation im Panel zeigte immer "1 von 1".
            var gesamt = await abfrage.CountAsync(cancellationToken);

            var items = await abfrage
                // Aufgabe UND deren Kategorie: die Zeile nennt beide. Fehlte das
                // Include, stünde dort ein leerer Name statt einer Fehlermeldung
                // - dieselbe stille Fehlerquelle wie bei GetByIdAsync.
                .Include(s => s.Task)
                    .ThenInclude(t => t.Category)
                .Include(s => s.EvaluationResult)
                .OrderByDescending(s => s.SubmittedAt)
                // Zweites Kriterium, damit die Reihenfolge TOTAL ist. Ohne das
                // entscheidet die Datenbank bei gleichem Zeitstempel, und zwar
                // je Abfrage neu: dieselbe Zeile könnte auf Seite 1 und auf
                // Seite 2 auftauchen, während eine andere ganz ausfällt.
                // Über HTTP kollidieren die Zeitstempel praktisch nie, ein
                // Seed-Skript legt seine Zeilen aber im selben Augenblick an.
                .ThenByDescending(s => s.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, gesamt);
        }

        public async Task<Submission?> GetWithFilesAsync(Guid id, CancellationToken cancellationToken)
        {
            // Zwei Gründe, warum das nicht GetByIdAsync ist:
            //
            // Erstens lädt die den kompletten Aufgabengraphen mit — Testfälle,
            // JUnit-Dateien, Gewichte, Vertrag. Die Auswertung braucht das, eine
            // Leseansicht nicht.
            //
            // Zweitens trägt sie Änderungsverfolgung. Hier wird nur gelesen.
            //
            // Was hier fehlte, bliebe still leer: die Kopfzeile nennte keinen
            // Titel, und die Ansicht sähe funktionsfähig aus - dieselbe stille
            // Fehlerquelle wie schon zweimal an dieser Klasse.
            return await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Files)
                .Include(s => s.Task)
                    .ThenInclude(t => t.Category)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task AddAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Submission submission)
        {
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }

        public async Task<Submission?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Submissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<List<Guid>> GetIdsByStatusAsync(
            IReadOnlyList<SubmissionStatus> statuses,
            CancellationToken cancellationToken)
        {
            return await _context.Submissions
                .AsNoTracking()
                .Where(s => statuses.Contains(s.Status))
                .OrderBy(s => s.SubmittedAt)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateStatusAsync(
            Guid id,
            SubmissionStatus status,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            await _context.Submissions
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(s => s.Status, status)
                        .SetProperty(s => s.ErrorMessage, errorMessage),
                    cancellationToken);
        }
    }
}