using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Evaluation.Models
{
    // Alles, was ein Checker über einen Auswertungslauf wissen muss. Wird vom
    // JavaAnalyzer angelegt und durch die Checker-Pipeline gereicht.
    public class EvaluationContext
    {
        public required Submission Submission { get; init; }

        // Die Aufgabe hinter der Abgabe - trägt Modus, Konsolen-Testfälle,
        // JUnit-Dateien und die aufgabenspezifischen Gewichte.
        public required TaskItem Task { get; init; }

        // Temporäres Verzeichnis des Laufs. Gehört dem JavaAnalyzer, der es
        // auch dann wieder löscht, wenn ein Checker eine Exception wirft.
        public required string WorkingDirectory { get; init; }

        public IReadOnlyList<SubmissionFile> Files { get; init; } = [];

        // Wird vom CompilabilityChecker gesetzt, sobald javac gelaufen ist.
        // Deshalb ist die Reihenfolge in IEvaluationChecker.Order verbindlich:
        // wer die Abgabe ausführen will, muss nach ihm laufen.
        public CompilationResult? Compilation { get; set; }
    }
}
