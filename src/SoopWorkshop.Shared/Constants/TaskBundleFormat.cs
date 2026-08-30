namespace SoopWorkshop.Shared.Constants
{
    public static class TaskBundleFormat
    {
        // Version 1: Kategorien mit Symbol, Aufgaben mit Vertrag über mehrere
        // Klassen, Konsolen-Testfälle, JUnit-Dateien und Gewichte.
        //
        // Wird nur erhöht, wenn eine ältere Datei nicht mehr gelesen werden
        // kann. Ein zusätzliches Feld allein ist kein Grund - das fällt beim
        // Einlesen einfach auf seinen Standardwert.
        public const int CurrentVersion = 1;

        // Vorschlag für den Dateinamen beim Herunterladen.
        public const string FileNamePrefix = "soop-bestand";
    }
}
