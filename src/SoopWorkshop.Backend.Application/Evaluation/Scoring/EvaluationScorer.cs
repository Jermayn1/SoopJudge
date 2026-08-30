using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Constants;

namespace SoopWorkshop.Backend.Application.Evaluation.Scoring
{
    // Punktesystem v2. Reine Funktion ohne Datenbank und ohne Prozesse - genau
    // deshalb lässt sich die Bewertung vollständig durchtesten.
    //
    // Regeln:
    //  1. Nur anwendbare Kategorien zählen. Ihre Gewichte werden auf die
    //     Gesamtpunktzahl normiert, das Gewicht weggefallener Kategorien verteilt
    //     sich damit von selbst auf die übrigen - keine Gratispunkte.
    //  2. Kategoriepunkte = erreichbare Punkte x (bestandene Teilprüfungen /
    //     Teilprüfungen gesamt), gerechnet in double.
    //  3. Gerundet wird nach größtem Rest, damit die Summe exakt aufgeht. Die
    //     alte Ganzzahl-Division hat bei 65/3 drei Punkte unterschlagen.
    //  4. Volle Punkte gibt es nur, wenn alle Teilprüfungen bestanden sind.
    public static class EvaluationScorer
    {
        public static IReadOnlyList<CategoryResult> Score(IReadOnlyList<CategoryScoreInput> inputs)
        {
            if (inputs.Count == 0)
                return [];

            // Nach Anzeigereihenfolge sortieren, bevor gerechnet wird: das legt
            // zugleich fest, wer bei gleichem Rest den zusätzlichen Punkt bekommt,
            // und macht das Ergebnis damit reproduzierbar.
            var ordered = inputs
                .OrderBy(input => EvaluationCategoryOrder.Of(input.Category))
                .ToList();

            Validate(ordered);

            var maxPoints = DistributeMaxPoints(ordered);
            var points = DistributePoints(ordered, maxPoints);

            return [.. ordered.Select((input, index) => BuildCategoryResult(input, points[index], maxPoints[index]))];
        }

        // Fehler in der Aufgabenkonfiguration dürfen nicht still zu einer
        // veränderten Note führen - lieber laut scheitern.
        private static void Validate(List<CategoryScoreInput> inputs)
        {
            foreach (var input in inputs)
            {
                if (input.Weight <= 0)
                    throw new InvalidOperationException(
                        $"Die Kategorie {input.Category} hat das Gewicht {input.Weight}. " +
                        "Gewichte muessen groesser als 0 sein.");

                if (input.Results.Count == 0)
                    throw new InvalidOperationException(
                        $"Die Kategorie {input.Category} gilt als anwendbar, hat aber keine Teilpruefung geliefert. " +
                        "Entweder liefert der Checker ein Ergebnis, oder er meldet sich als nicht anwendbar.");
            }
        }

        // Erreichbare Punkte je Kategorie: Gewichte auf die Gesamtpunktzahl
        // normieren und per größtem Rest auf ganze Zahlen bringen.
        private static int[] DistributeMaxPoints(List<CategoryScoreInput> inputs)
        {
            var totalWeight = inputs.Sum(input => input.Weight);

            var exact = inputs
                .Select(input => input.Weight / totalWeight * EvaluationScoring.TotalPoints)
                .ToArray();

            return LargestRemainder(exact, EvaluationScoring.TotalPoints, _ => true);
        }

        private static int[] DistributePoints(List<CategoryScoreInput> inputs, int[] maxPoints)
        {
            var points = new int[inputs.Count];
            var exact = new double[inputs.Count];

            // Ganz bestanden und ganz durchgefallen stehen sofort fest. Nur die
            // teilweise bestandenen Kategorien nehmen an der Restverteilung teil -
            // sonst könnte Runden aus "fast alles richtig" volle Punkte machen.
            var partial = new bool[inputs.Count];

            for (var index = 0; index < inputs.Count; index++)
            {
                var results = inputs[index].Results;
                var passed = results.Count(result => result.Passed);

                exact[index] = maxPoints[index] * (double)passed / results.Count;

                if (passed == results.Count)
                    points[index] = maxPoints[index];
                else if (passed == 0)
                    points[index] = 0;
                else
                {
                    // Deckel bei MaxPoints - 1: wer nicht alles bestanden hat,
                    // darf durch Aufrunden nie die volle Punktzahl erreichen.
                    points[index] = Math.Min((int)Math.Floor(exact[index]), maxPoints[index] - 1);
                    partial[index] = true;
                }
            }

            var target = (int)Math.Round(exact.Sum(), MidpointRounding.AwayFromZero);
            var remainder = target - points.Sum();

            if (remainder <= 0)
                return points;

            var candidates = Enumerable.Range(0, inputs.Count)
                .Where(index => partial[index] && points[index] < maxPoints[index] - 1)
                .OrderByDescending(index => exact[index] - Math.Floor(exact[index]))
                .ThenBy(index => index);

            foreach (var index in candidates)
            {
                if (remainder == 0)
                    break;

                points[index]++;
                remainder--;
            }

            return points;
        }

        // Verteilt so viele ganze Punkte, dass die Summe exakt das Ziel trifft:
        // erst abrunden, dann den Rest an die größten Nachkommaanteile.
        private static int[] LargestRemainder(double[] exact, int target, Func<int, bool> canReceive)
        {
            var result = exact.Select(value => (int)Math.Floor(value)).ToArray();
            var remainder = target - result.Sum();

            var candidates = Enumerable.Range(0, exact.Length)
                .Where(canReceive)
                .OrderByDescending(index => exact[index] - Math.Floor(exact[index]))
                .ThenBy(index => index)
                .ToArray();

            for (var step = 0; step < remainder && candidates.Length > 0; step++)
            {
                result[candidates[step % candidates.Length]]++;
            }

            return result;
        }

        private static CategoryResult BuildCategoryResult(CategoryScoreInput input, int points, int maxPoints)
        {
            var categoryResult = new CategoryResult
            {
                Id = Guid.NewGuid(),
                Category = input.Category,
                Points = points,
                MaxPoints = maxPoints,
                Passed = input.Results.All(result => result.Passed),
                ErrorTip = input.ErrorTip ?? string.Empty
            };

            // Reihenfolge festhalten, in der die Checker die Teilprüfungen
            // geliefert haben - die Datenbank gibt sie sonst beliebig zurück.
            var order = 0;
            foreach (var result in input.Results)
            {
                result.CategoryResultId = categoryResult.Id;
                result.Order = order++;
                categoryResult.TestCaseResults.Add(result);
            }

            return categoryResult;
        }
    }
}
