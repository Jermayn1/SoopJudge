using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Evaluation.Scoring
{
    // Eine anwendbare Kategorie mit ihrem Gewicht und allen Teilprüfungen, die
    // die Checker dazu geliefert haben. Nicht anwendbare Kategorien tauchen hier
    // gar nicht erst auf - ihr Gewicht verteilt sich dadurch auf die übrigen.
    public sealed record CategoryScoreInput(
        EvaluationCategory Category,
        double Weight,
        IReadOnlyList<TestCaseResult> Results,
        string? ErrorTip);
}
