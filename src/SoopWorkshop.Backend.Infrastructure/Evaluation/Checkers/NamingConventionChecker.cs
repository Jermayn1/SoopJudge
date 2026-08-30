using System.Text.RegularExpressions;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Prüft auf Nameconventions
    // Klassennamen in PascalCase
    // Variablen und Methodennamen in lowerCamelCase
    // Teilprüfung der Sammelkategorie Clean Code.
    public class NamingConventionChecker : IEvaluationChecker
    {
        private static readonly Regex ClassDeclaration = new(@"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        // Erkennt Identifier wie "mein_wert". SCREAMING_SNAKE_CASE Konstanten (z.B. MAX_VALUE)
        // werden bewusst nicht erfasst, da diese in Java üblich und korrekt sind.
        private static readonly Regex SnakeCaseIdentifier = new(@"\b[a-z][a-z0-9]*_[a-z0-9_]*\b", RegexOptions.Compiled);

        public EvaluationCategory Category => EvaluationCategory.CleanCode;

        public int Order => EvaluationCheckerOrder.NamingConventions;

        public bool IsApplicable(EvaluationContext context) => true;

        public Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            // Nur echter Code wird geprüft. Was in Kommentaren oder in Ausgaben
            // steht, sagt nichts über die Benennung im Programm aus.
            var code = string.Join(
                "\n",
                context.Files.Select(file => JavaSourceText.StripCommentsAndLiterals(file.Content)));

            var classNamesValid = CheckClassNames(code);
            var noSnakeCase = !SnakeCaseIdentifier.IsMatch(code);

            var results = new[]
            {
                new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = "Klassennamen sind in PascalCase geschrieben",
                    Passed = classNamesValid
                },
                new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = "Variablen- und Methodennamen sind in camelCase geschrieben",
                    Passed = noSnakeCase
                }
            };

            var outcome = classNamesValid && noSnakeCase
                ? CheckerOutcome.Of(results)
                : CheckerOutcome.WithTip(
                    "Klassen werden in PascalCase benannt (z.B. 'MeineKlasse'), Variablen und Methoden in camelCase (z.B. 'meineVariable').",
                    results);

            return Task.FromResult(outcome);
        }

        private static bool CheckClassNames(string content)
        {
            var matches = ClassDeclaration.Matches(content);

            if (matches.Count == 0)
                return true;

            return matches.All(m => char.IsUpper(m.Groups[1].Value[0]));
        }
    }
}
