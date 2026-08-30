namespace SoopWorkshop.Backend.Application.Evaluation.Models
{
    // Ergebnis eines externen Prozesses.
    // TimedOut und ExecutableNotFound sind bewusst eigene Kennzeichen und keine
    // Sonder-Exitcodes: der Aufrufer muss beide Fälle unterschiedlich erklären.
    public record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError,
        bool TimedOut,
        bool ExecutableNotFound)
    {
        public bool Success => !TimedOut && !ExecutableNotFound && ExitCode == 0;
    }
}
