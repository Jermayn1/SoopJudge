using System.Text.RegularExpressions;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Prüft, ob die Abgabe die von der Aufgabe geforderten Namen überhaupt
    // verwendet - die Klassen und die Methoden darin.
    //
    // Warum eigens: Java erzwingt nur, dass Dateiname und Klassenname
    // zusammenpassen, nicht dass sie heißen wie gefordert. Eine Aufgabe verlangt
    // 'Main' mit 'addiere', jemand gibt 'Rechner.java' mit 'class Rechner' ab -
    // das kompiliert, die Konsolen-Testfälle laufen durch, und die Abgabe besteht.
    // Bei JUnit fällt es zwar auf, aber erst als Compilerfehler.
    //
    // Seit dem Umbau auf mehrere Klassen wird die Methode im Rumpf IHRER Klasse
    // gesucht. Vorher lief die Suche über den gesamten Quelltext: 'einzahlen'
    // zählte auch dann als vorhanden, wenn es in 'Kunde' statt in 'Konto' stand.
    // Für die OOP-Aufgaben am Ende des Workshops war das zu grob.
    //
    // Läuft vor dem Kompilieren und braucht nur den Quelltext.
    public class ContractChecker : IEvaluationChecker
    {
        // Teil der Kompilierbarkeit: hier geht es darum, ob der Code überhaupt
        // zur Aufgabe passt - nicht um Stil und nicht um das Ergebnis.
        public EvaluationCategory Category => EvaluationCategory.Compilability;

        public int Order => EvaluationCheckerOrder.Contract;

        // Nur wenn die Aufgabe überhaupt Namen vorgibt.
        public bool IsApplicable(EvaluationContext context) => context.Task.ExpectedTypes.Count > 0;

        public Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            // Nur echter Code: eine Klasse, die bloß im Kommentar erwähnt wird,
            // ist nicht deklariert. Das Bereinigen ist zugleich die Voraussetzung
            // dafür, dass unten Klammern gezählt werden dürfen.
            var code = string.Join(
                "\n",
                context.Files.Select(file => JavaSourceText.StripCommentsAndLiterals(file.Content)));

            var declaredNames = JavaTypeBodies.DeclaredNames(code);
            var results = new List<TestCaseResult>();
            var missing = new List<string>();

            foreach (var type in context.Task.ExpectedTypes.OrderBy(type => type.Order))
            {
                var body = JavaTypeBodies.BodyOf(code, type.Name);
                var found = body is not null;

                results.Add(new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = $"Die Klasse „{type.Name}“ ist vorhanden",
                    ExpectedOutput = type.Name,
                    ActualOutput = found
                        ? type.Name
                        : declaredNames.Count == 0 ? "keine Klasse gefunden" : string.Join(", ", declaredNames),
                    Passed = found
                });

                if (!found)
                    missing.Add($"die Klasse „{type.Name}“");

                foreach (var method in type.Methods.OrderBy(method => method.Order))
                {
                    // Fehlt die Klasse, fehlt auch ihre Methode. Das trotzdem als
                    // eigene Teilprüfung zu zeigen ist richtig: die Aufgabe hat
                    // sie verlangt, und eine verschwiegene Prüfung wäre eine
                    // stillschweigend mildere Bewertung.
                    var declared = found && DeclaresMethod(body!, method.Name);

                    results.Add(new TestCaseResult
                    {
                        Id = Guid.NewGuid(),
                        // Die Klasse gehört in die Überschrift - sonst stehen bei
                        // mehreren Klassen zwei gleichnamige Prüfungen nebeneinander.
                        Description = $"Die Methode „{method.Name}“ steht in „{type.Name}“",
                        ExpectedOutput = method.Signature,
                        ActualOutput = declared
                            ? method.Signature
                            : found ? "in dieser Klasse nicht gefunden" : $"Klasse „{type.Name}“ fehlt",
                        Passed = declared
                    });

                    if (!declared && found)
                        missing.Add($"die Methode „{method.Signature}“ in „{type.Name}“");
                }
            }

            if (missing.Count == 0)
                return Task.FromResult(CheckerOutcome.Of([.. results]));

            return Task.FromResult(CheckerOutcome.WithTip(
                $"Deine Abgabe enthält {string.Join(" und ", missing)} nicht. " +
                "Die Aufgabenstellung gibt die Namen genau vor — achte auf Schreibweise " +
                "und Groß-/Kleinschreibung.",
                [.. results]));
        }

        // Geprüft wird die reine Anwesenheit des Namens vor einer Klammer.
        // Ist-Verhalten: ein bloßer Aufruf 'addiere(1, 2)' im selben Rumpf
        // zählt bereits als Treffer. Das ist hingenommen - wer die Methode
        // aufruft, hat sie meistens auch. Die exakte Signatur prüft ohnehin der
        // Java-Compiler beim Übersetzen der JUnit-Datei.
        private static bool DeclaresMethod(string body, string methodName) =>
            !string.IsNullOrWhiteSpace(methodName)
            && Regex.IsMatch(body, $@"(?<![.\w]){Regex.Escape(methodName)}\s*\(");
    }
}
