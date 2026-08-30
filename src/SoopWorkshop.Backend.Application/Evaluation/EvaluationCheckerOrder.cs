namespace SoopWorkshop.Backend.Application.Evaluation
{
    // Ausführungsreihenfolge der Checker. Die Abstände lassen Platz, damit eine
    // neue Prüfung dazwischen passt, ohne bestehende Werte zu verschieben.
    //
    // Kompiliert wird zuerst: der CompilabilityChecker legt das Kompilierergebnis
    // im EvaluationContext ab, alles was die Abgabe ausführt braucht es.
    // Die Anzeigereihenfolge ist eine andere und steht in EvaluationCategoryOrder.
    public static class EvaluationCheckerOrder
    {
        // Vor dem Kompilieren: sagt "die Klasse muss Main heißen" verständlicher,
        // als javac es je könnte, und braucht dafür nur den Quelltext.
        public const int Contract = 5;

        public const int Compilability = 10;
        public const int CharacterSet = 20;
        public const int NamingConventions = 30;
        public const int TestCases = 40;
        public const int UnitTests = 50;
    }
}
