using System.Runtime.InteropServices;

namespace SoopWorkshop.Tests.Helpers
{
    // Legt kleine Skripte in einem temporären Verzeichnis an, damit der ProcessRunner
    // gegen echte Prozesse geprüft werden kann — ohne installiertes JDK.
    // Windows und Unix brauchen unterschiedliche Skriptsprachen, deshalb bekommt
    // jeder Testfall beide Varianten übergeben.
    public sealed class ScriptFactory : IDisposable
    {
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public string WorkingDirectory { get; }

        public string Interpreter => IsWindows ? "cmd.exe" : "/bin/sh";

        public ScriptFactory()
        {
            WorkingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(WorkingDirectory);
        }

        // Gibt die Argumentliste zurück, mit der das Skript ausgeführt wird.
        public IReadOnlyList<string> Create(string windowsScript, string unixScript)
        {
            var fileName = IsWindows ? "script.bat" : "script.sh";
            var path = Path.Combine(WorkingDirectory, fileName);

            File.WriteAllText(path, IsWindows ? windowsScript : unixScript);

            return IsWindows ? ["/c", path] : [path];
        }

        public void Dispose()
        {
            if (Directory.Exists(WorkingDirectory))
            {
                Directory.Delete(WorkingDirectory, recursive: true);
            }
        }
    }
}
