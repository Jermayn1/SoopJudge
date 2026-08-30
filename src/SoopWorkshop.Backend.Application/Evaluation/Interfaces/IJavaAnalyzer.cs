using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Evaluation.Interfaces
{
    public interface IJavaAnalyzer
    {
        // Wertet die hochgeladenen .java-Dateien einer Abgabe aus. Die Aufgabe mit
        // ihren Testfällen, Modi und Gewichten hängt an submission.Task und wird
        // vom Repository mitgeladen - deshalb braucht es keinen zweiten Parameter.
        Task<EvaluationResult> AnalyzeAsync(Submission submission, CancellationToken cancellationToken);
    }
}
