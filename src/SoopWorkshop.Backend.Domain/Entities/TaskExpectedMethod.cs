namespace SoopWorkshop.Backend.Domain.Entities
{
    // Eine Methode, die die Aufgabe verlangt. Der Admin schreibt die Signatur so
    // auf, wie sie in der Aufgabenstellung steht; der Name wird daraus abgeleitet.
    //
    // Hängt an der Klasse, nicht an der Aufgabe: nur so lässt sich prüfen, ob
    // die Methode auch dort steht, wo sie hingehört.
    public class TaskExpectedMethod
    {
        public Guid Id { get; set; }
        public Guid TaskExpectedTypeId { get; set; }

        // Vollständig, für Anzeige und Fehlermeldung:
        // "public static int addiere(int ersteZahl, int zweiteZahl)"
        public string Signature { get; set; } = string.Empty;

        // Der reine Methodenname, gegen den geprüft wird: "addiere".
        // Bewusst nur der Name: die genauen Parametertypen prüft die
        // JUnit-Kompilierung ohnehin exakt, und ein Regex über Java-Quelltext
        // würde daran nur unzuverlässig scheitern.
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        public TaskExpectedType Type { get; set; } = null!;
    }
}
