namespace SoopWorkshop.Backend.Domain.Entities
{
    // Eine JUnit-Quelldatei, die der Admin zu einer Aufgabe hinterlegt. Sie wird
    // zusammen mit der Abgabe kompiliert und ausgeführt.
    public class TaskUnitTestFile
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }

        // Muss auf .java enden und zum Klassennamen darin passen - daraus leitet
        // der JUnitChecker die auszuführende Testklasse ab.
        public string FileName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }

        // Standard false: die Tests zu sehen ist lehrreich, verleitet aber dazu,
        // auf den Test hin zu schreiben statt die Aufgabe zu lösen. Pro Datei
        // entscheidbar.
        public bool IsVisibleToParticipant { get; set; }

        public TaskItem Task { get; set; } = null!;
    }
}
