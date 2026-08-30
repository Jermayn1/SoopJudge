using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Evaluation.Interfaces
{
    // Eine Prüfung innerhalb der Auswertung. Neue Prüfungen werden nur noch
    // registriert, der JavaAnalyzer muss dafür nicht mehr angefasst werden.
    public interface IEvaluationChecker
    {
        // Mehrere Checker dürfen dieselbe Kategorie liefern - Clean Code besteht
        // genau so aus mehreren unabhängigen Teilprüfungen.
        EvaluationCategory Category { get; }

        // Ausführungsreihenfolge, nicht Anzeigereihenfolge (die steht in
        // EvaluationCategoryOrder). Wer das Kompilierergebnis aus dem Kontext
        // braucht, muss hinter dem CompilabilityChecker liegen.
        int Order { get; }

        // Ob die Prüfung für diese Aufgabe überhaupt gilt.
        //
        // WICHTIG: Diese Frage hängt allein an der Aufgabendefinition - Modus,
        // hinterlegte Testfälle, hinterlegte JUnit-Dateien. Niemals am Ergebnis
        // des Laufs. Würde eine nicht kompilierende Abgabe hier als "nicht
        // anwendbar" gelten, fiele ihre Kategorie aus der Wertung, das Gewicht
        // würde auf die übrigen verteilt - und kaputter Code bekäme eine
        // bessere Note als halb funktionierender.
        bool IsApplicable(EvaluationContext context);

        Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken);
    }
}
