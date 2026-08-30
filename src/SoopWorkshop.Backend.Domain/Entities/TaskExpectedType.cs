namespace SoopWorkshop.Backend.Domain.Entities
{
    // Eine Klasse, die die Aufgabe verlangt - samt der Methoden, die IN DIESER
    // Klasse stehen müssen.
    //
    // Die Zuordnung Methode -> Klasse ist wesentlich, nicht schmückend: bei den
    // OOP-Aufgaben hängen mehrere Klassen voneinander ab, und "einzahlen" gehört
    // zu 'Konto' und nicht irgendwohin. Eine flache Methodenliste über die ganze
    // Abgabe zählte die Methode auch dann als vorhanden, wenn sie in einer ganz
    // anderen Klasse steht.
    public class TaskExpectedType
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }

        // Der geforderte Name: "Konto". Ob es Klasse, Interface, Enum oder Record
        // sein soll, steht in der Aufgabenbeschreibung - geprüft wird der Name.
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        public TaskItem Task { get; set; } = null!;

        public ICollection<TaskExpectedMethod> Methods { get; set; } = [];
    }
}
