namespace SoopWorkshop.Shared.DTOs.Submissions
{
    // Eine Seite der Abgaben-Übersicht.
    //
    // Total gehört dazu und nicht bloß die Zeilen: ohne die Gesamtzahl kann
    // das Panel nicht sagen, ob es noch eine Seite gibt. Es könnte es aus
    // "weniger Zeilen als angefordert" erraten - das stimmt aber genau dann
    // nicht, wenn die letzte Seite zufällig voll ist.
    public class SubmissionPageDto
    {
        public List<SubmissionListItemDto> Items { get; set; } = [];

        public int Total { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}
