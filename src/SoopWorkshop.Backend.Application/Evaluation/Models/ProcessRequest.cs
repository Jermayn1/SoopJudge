namespace SoopWorkshop.Backend.Application.Evaluation.Models
{
    // Beschreibt einen auszuführenden externen Prozess (javac, java, später JUnit).
    // Argumente bewusst als Liste, nicht als Zeichenkette — das Quoting übernimmt
    // die Prozess-API, damit Leerzeichen in Pfaden nicht die Befehlszeile zerlegen.
    public record ProcessRequest(
        string FileName,
        IReadOnlyList<string> Arguments,
        string WorkingDirectory,
        string? StandardInput,
        TimeSpan Timeout);
}
