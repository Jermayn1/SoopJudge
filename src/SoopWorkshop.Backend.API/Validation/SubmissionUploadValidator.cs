using SoopWorkshop.Shared.Constants;

namespace SoopWorkshop.Backend.API.Validation
{
    // Serverseitige Prüfung der hochgeladenen Dateien. Das Frontend blockt zwar
    // früher, verbindlich ist aber nur, was hier geprüft wird.
    public static class SubmissionUploadValidator
    {
        // Leere Liste bedeutet: gültig.
        public static List<string> Validate(IReadOnlyList<IFormFile> files)
        {
            var errors = new List<string>();

            if (files.Count == 0)
            {
                errors.Add("Es wurde keine Datei hochgeladen.");
                return errors;
            }

            if (files.Count > SubmissionUploadLimits.MaxFileCount)
            {
                errors.Add($"Es sind höchstens {SubmissionUploadLimits.MaxFileCount} Dateien erlaubt.");
            }

            var seenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var fileName = file.FileName;

                if (!IsSafeFileName(fileName))
                {
                    errors.Add($"„{fileName}“ ist kein gültiger Dateiname.");
                    continue;
                }

                if (!fileName.EndsWith(SubmissionUploadLimits.AllowedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"„{fileName}“ ist keine {SubmissionUploadLimits.AllowedExtension}-Datei.");
                }

                if (file.Length == 0)
                {
                    errors.Add($"„{fileName}“ ist leer.");
                }
                else if (file.Length > SubmissionUploadLimits.MaxFileSizeBytes)
                {
                    var maxKilobytes = SubmissionUploadLimits.MaxFileSizeBytes / 1024;
                    errors.Add($"„{fileName}“ ist größer als {maxKilobytes} KB.");
                }

                // Zwei Dateien mit gleichem Namen würden sich im Arbeitsverzeichnis
                // gegenseitig überschreiben — ohne dass es jemand merkt.
                if (!seenFileNames.Add(fileName))
                {
                    errors.Add($"„{fileName}“ wurde mehrfach hochgeladen.");
                }
            }

            return errors;
        }

        // Der Dateiname landet als Pfadbestandteil im Arbeitsverzeichnis. Erlaubt ist
        // deshalb nur ein reiner Name — keine Verzeichnisse, keine Sonderzeichen.
        private static bool IsSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // Beide Trenner ausdrücklich, denn unter Linux gilt '\' als gültiges
            // Zeichen und rutschte sonst durch.
            if (fileName.Contains('/') || fileName.Contains('\\'))
                return false;

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;

            return Path.GetFileName(fileName) == fileName;
        }
    }
}
