using System.Text.RegularExpressions;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Prüft die Namen, die der Teilnehmer vergibt: Klassen, Methoden,
    // Parameter, Variablen, Konstanten. Teilprüfung der Sammelkategorie
    // Clean Code.
    //
    // Das Bereinigen ist keine Vorsichtsmassnahme, sondern die Regel selbst.
    // Über die rohe Datei kostet "Größe" in einer Ausgabe denselben Punkt wie
    // im Variablennamen, und eine Aufgabe mit vorgeschriebenem Umlaut wäre
    // nicht mehr voll lösbar.
    public class CharacterSetChecker : IEvaluationChecker
    {
        private static readonly Regex ForbiddenCharacters = new(@"[äöüÄÖÜß]", RegexOptions.Compiled);

        public EvaluationCategory Category => EvaluationCategory.CleanCode;

        public int Order => EvaluationCheckerOrder.CharacterSet;

        // Gilt für jede Aufgabe - Clean Code wird immer bewertet.
        public bool IsApplicable(EvaluationContext context) => true;

        public Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            var hasForbiddenCharacters = context.Files.Any(
                file => ForbiddenCharacters.IsMatch(JavaSourceText.StripCommentsAndLiterals(file.Content)));

            var result = new TestCaseResult
            {
                Id = Guid.NewGuid(),
                Description = "Klassen-, Methoden- und Variablennamen kommen ohne Umlaute und ohne ß aus",
                Passed = !hasForbiddenCharacters
            };

            var outcome = hasForbiddenCharacters
                ? CheckerOutcome.WithTip(
                    "Umlaute (ä, ö, ü) und das ß-Zeichen gehören nicht in die Namen, die du selbst vergibst — also nicht in Klassen-, Methoden-, Parameter- und Variablennamen. Schreibe sie dort aus: „ae“, „oe“, „ue“, „ss“. In Ausgabetexten, Zeichenketten und Kommentaren sind die Zeichen erlaubt.",
                    result)
                : CheckerOutcome.Of(result);

            return Task.FromResult(outcome);
        }
    }
}
