using System.Text.RegularExpressions;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Prüft, ob die eingereichten Dateien Umlaute oder ß beinhalten.
    // Teilprüfung der Sammelkategorie Clean Code.
    public class CharacterSetChecker : IEvaluationChecker
    {
        private static readonly Regex ForbiddenCharacters = new(@"[äöüÄÖÜß]", RegexOptions.Compiled);

        public EvaluationCategory Category => EvaluationCategory.CleanCode;

        public int Order => EvaluationCheckerOrder.CharacterSet;

        // Gilt für jede Aufgabe - Clean Code wird immer bewertet.
        public bool IsApplicable(EvaluationContext context) => true;

        public Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            var hasForbiddenCharacters = context.Files.Any(file => ForbiddenCharacters.IsMatch(file.Content));

            var result = new TestCaseResult
            {
                Id = Guid.NewGuid(),
                Description = "Der Code kommt ohne Umlaute und ohne ß aus",
                Passed = !hasForbiddenCharacters
            };

            var outcome = hasForbiddenCharacters
                ? CheckerOutcome.WithTip(
                    "Vermeide Umlaute (ä, ö, ü) und das ß-Zeichen im Code — auch in Kommentaren und in Ausgabetexten. Nutze stattdessen z. B. „ae“, „oe“, „ue“, „ss“.",
                    result)
                : CheckerOutcome.Of(result);

            return Task.FromResult(outcome);
        }
    }
}
