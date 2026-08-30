using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Transfer
{
    // Der gesamte Aufgabenbestand als eine Datei.
    //
    // Gedacht als Deployment-Weg: der Bestand wird zuhause gepflegt und als
    // Datei auf den Server gebracht. Abgaben und Auswertungen sind bewusst NICHT
    // enthalten - das sind Workshop-Daten, keine Konfiguration.
    public class TaskBundleDto
    {
        // Ganzzahl und keine Zeichenkette: ein späteres Format soll erkannt
        // werden, statt still falsch gelesen zu werden.
        public int FormatVersion { get; set; } = TaskBundleFormat.CurrentVersion;

        public DateTimeOffset ExportedAt { get; set; }

        public List<TaskBundleCategoryDto> Categories { get; set; } = [];
    }

    public class TaskBundleCategoryDto
    {
        // Die Id wandert mit. Dadurch erkennt ein erneuter Import dieselbe
        // Kategorie wieder, statt sie zu verdoppeln - und ein Umbenennen bricht
        // nichts.
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public string IconName { get; set; } = string.Empty;

        public List<TaskBundleTaskDto> Tasks { get; set; } = [];
    }

    public class TaskBundleTaskDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Difficulty Difficulty { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public EvaluationMode EvaluationMode { get; set; }

        public List<string> Hints { get; set; } = [];

        // Die Kinder tragen keine eigene Id: sie werden beim Import als Block
        // ersetzt, eine Identität bräuchten sie nur zum Abgleichen.
        public List<TaskBundleExpectedTypeDto> ExpectedTypes { get; set; } = [];
        public List<TaskBundleTestDto> Tests { get; set; } = [];
        public List<TaskBundleUnitTestFileDto> UnitTestFiles { get; set; } = [];
        public List<TaskBundleWeightDto> Weights { get; set; } = [];
    }

    public class TaskBundleExpectedTypeDto
    {
        public string Name { get; set; } = string.Empty;

        // Vollständige Signaturen; der geprüfte Name wird daraus abgeleitet.
        public List<string> Methods { get; set; } = [];
    }

    public class TaskBundleTestDto
    {
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class TaskBundleUnitTestFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisibleToParticipant { get; set; }
    }

    public class TaskBundleWeightDto
    {
        public EvaluationCategory Category { get; set; }
        public double Weight { get; set; }
    }
}
