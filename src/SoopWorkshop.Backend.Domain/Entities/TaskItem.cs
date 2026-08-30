using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid TaskCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public int Order { get; set; }
    public bool IsVisible { get; set; }

    // Womit geprüft wird. Standard ist die Konsolenprüfung, mit der die
    // frühen Aufgaben auskommen.
    public EvaluationMode EvaluationMode { get; set; } = EvaluationMode.ConsoleOnly;

    public TaskCategory Category { get; set; } = null!;
    public ICollection<TaskHint> Hints { get; set; } = [];
    public ICollection<TaskTest> Tests { get; set; } = [];
    public ICollection<TaskUnitTestFile> UnitTestFiles { get; set; } = [];

    // Der Vertrag zwischen Aufgabe und Abgabe: welche Klassen es geben muss und
    // welche Methoden in welcher davon. Ohne ihn steht nur im Fließtext der
    // Beschreibung, wie die Klassen heißen sollen - und eine Abgabe mit
    // falschen Namen besteht klaglos, solange sie kompiliert.
    //
    // Leer, wenn die Aufgabe keine Namen vorgibt.
    public ICollection<TaskExpectedType> ExpectedTypes { get; set; } = [];

    public ICollection<Submission> Submissions { get; set; } = [];

    // Leer, solange die Standardgewichte aus der Konfiguration gelten sollen.
    public ICollection<TaskCategoryWeight> CategoryWeights { get; set; } = [];
}