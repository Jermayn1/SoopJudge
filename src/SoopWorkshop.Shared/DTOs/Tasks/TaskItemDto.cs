using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Tasks
{
    public class TaskItemDto
    {
        public Guid Id { get; set; }
        public Guid TaskCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Difficulty Difficulty { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public EvaluationMode EvaluationMode { get; set; }

        // Der Vertrag: welche Klassen es geben muss und welche Methoden in
        // welcher davon. Wird geprüft und dem Teilnehmer angezeigt.
        //
        // Leer, wenn die Aufgabe keine Namen vorgibt.
        public List<TaskExpectedTypeDto> ExpectedTypes { get; set; } = [];

        public List<TaskHintDto> Hints { get; set; } = [];

        // Nur die Dateien, die für Teilnehmer freigeschaltet sind. Im
        // Admin-Bereich kommen sie über den eigenen Endpunkt vollständig.
        public List<TaskUnitTestFileDto> VisibleUnitTestFiles { get; set; } = [];
    }
}