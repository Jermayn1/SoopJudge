using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Evaluation.Models
{
    // Was ein Checker zurückgibt: bestandene und nicht bestandene Teilprüfungen,
    // dazu ein Hinweis für den Teilnehmer.
    //
    // Bewusst ohne Punkte. Die Punkteberechnung gehört in den EvaluationScorer -
    // solange jeder Checker selbst gerechnet hat, gab es vier Stellen mit vier
    // Rundungsfehlern und keine Möglichkeit, pro Aufgabe zu gewichten.
    public sealed record CheckerOutcome(IReadOnlyList<TestCaseResult> Results, string? ErrorTip)
    {
        public static CheckerOutcome Of(params TestCaseResult[] results) =>
            new(results, null);

        public static CheckerOutcome WithTip(string errorTip, params TestCaseResult[] results) =>
            new(results, errorTip);
    }
}
