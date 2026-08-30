using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class TaskItemRepository : ITaskItemRepository
    {
        private readonly AppDbContext _context;

        public TaskItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskItem>> GetAllAsync()
        {
            return await _context.TaskItems
                .Include(t => t.Hints)
                .OrderBy(t => t.Order)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            return await _context.TaskItems
                .Include(t => t.Hints)
                .Include(t => t.Tests)
                .Include(t => t.UnitTestFiles)
                .Include(t => t.ExpectedTypes)
                    .ThenInclude(type => type.Methods)
                // CategoryWeights fehlte hier. Was nicht mitgeladen wird, sieht
                // die Auswertung als "nicht vorhanden" und bewertet entsprechend -
                // die stillste denkbare Fehlerquelle.
                .Include(t => t.CategoryWeights)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.TaskItems.AnyAsync(t => t.Id == id, cancellationToken);
        }

        public async Task AddAsync(TaskItem item)
        {
            _context.TaskItems.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskItem item)
        {
            // Kommt die Aufgabe aus GetByIdAsync, ist sie schon verfolgt - dann
            // erledigt die Änderungsverfolgung alles, auch das Anlegen neuer und
            // das Löschen entfernter Kindzeilen.
            //
            // Update() wäre dann nicht falsch, aber verschwenderisch: es setzt
            // die Aufgabenzeile auf Modified, und EF schreibt daraufhin alle ihre
            // Spalten statt nur der geänderten.
            //
            // Nur bei einer LOSGELOESTEN Aufgabe färbt Update() den ganzen
            // mitgegebenen Graphen und schreibt jede Kindzeile neu - da ist der
            // Aufruf aber nötig, sonst geht das Speichern still ins Leere. Beide
            // Fälle sind in TaskItemRepositoryTests nachgemessen.
            if (_context.Entry(item).State == EntityState.Detached)
                _context.TaskItems.Update(item);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _context.TaskItems.FindAsync(id);
            if (item is not null)
            {
                _context.TaskItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}