namespace SoopWorkshop.Shared.Constants
{
    // Grenzen für den Datei-Upload einer Abgabe.
    // Liegt in Shared, damit Frontend und Backend dieselben Werte benutzen —
    // das Frontend blockt früh, das Backend prüft verbindlich.
    public static class SubmissionUploadLimits
    {
        public const string AllowedExtension = ".java";

        public const int MaxFileCount = 10;

        public const long MaxFileSizeBytes = 1024 * 1024;

        // Obergrenze für den gesamten Request-Body.
        public const long MaxTotalSizeBytes = MaxFileCount * MaxFileSizeBytes;
    }
}
