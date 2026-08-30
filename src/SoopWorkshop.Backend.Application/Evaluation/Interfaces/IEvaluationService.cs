namespace SoopWorkshop.Backend.Application.Evaluation.Interfaces
{
    public interface IEvaluationService
    {
        // Beginnt die Auswertung einer Submission und speichert das Ergebnis
        Task EvaluateAsync(Guid submissionId, CancellationToken cancellationToken);
    }
}