using SoopWorkshop.Backend.Application.Evaluation.Models;

namespace SoopWorkshop.Tests.Helpers
{
    // Kurzformen für die vier Ausgänge eines Prozessaufrufs.
    public static class ProcessResultFactory
    {
        public static ProcessResult Success(string standardOutput = "", string standardError = "") =>
            new(0, standardOutput, standardError, TimedOut: false, ExecutableNotFound: false);

        public static ProcessResult Failure(string standardError, int exitCode = 1, string standardOutput = "") =>
            new(exitCode, standardOutput, standardError, TimedOut: false, ExecutableNotFound: false);

        public static ProcessResult TimedOut() =>
            new(-1, string.Empty, string.Empty, TimedOut: true, ExecutableNotFound: false);

        public static ProcessResult NotFound() =>
            new(-1, string.Empty, string.Empty, TimedOut: false, ExecutableNotFound: true);
    }
}
