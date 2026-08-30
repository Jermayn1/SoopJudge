using SoopWorkshop.Backend.Application.Evaluation.Models;

namespace SoopWorkshop.Backend.Application.Evaluation.Interfaces
{
    // Kapselt das Starten externer Prozesse. Dadurch sind die Checker ohne
    // installiertes JDK testbar, und die Ausführung lässt sich später
    // austauschen (z. B. gegen einen Container pro Abgabe).
    public interface IProcessRunner
    {
        Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);
    }
}
