namespace SoopWorkshop.Shared.DTOs.Submissions
{
    // Eine abgegebene .java-Datei mit ihrem vollständigen Inhalt.
    //
    // Ein Typ dieses Namens stand schon einmal hier und ist in Phase 0 als toter
    // Code geflogen — damals gab es keinen Aufrufer. Jetzt gibt es einen: die
    // Abgabe-Ansicht im Panel zeigt den Quelltext, damit sich eine Lösung im
    // Workshop besprechen lässt. Nicht wieder löschen, ohne dort nachzusehen.
    public class SubmissionFileDto
    {
        public string FileName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
