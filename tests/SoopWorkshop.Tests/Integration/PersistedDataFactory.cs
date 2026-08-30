using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Baut vollständige Objektbäume für die Integrationstests.
    /// </summary>
    /// <remarks>
    /// Bewusst "vollstaendig": ein Test, der nur die Aufgabe anlegt, kann nicht
    /// zeigen, ob GetByIdAsync ihre Kinder mitlädt. Die Include-Tests brauchen
    /// eine Aufgabe, an der jede einzelne Navigation etwas hängen hat - fehlt
    /// eine, ist der Test still wirkungslos.
    /// </remarks>
    public static class PersistedDataFactory
    {
        public static TaskCategory VollstaendigeKategorie(string name = "OOP")
        {
            var categoryId = Guid.NewGuid();

            return new TaskCategory
            {
                Id = categoryId,
                Name = name,
                Order = 1,
                IsVisible = true,
                IconName = "Layers",
                Tasks = [VollstaendigeAufgabe(categoryId)]
            };
        }

        public static TaskItem VollstaendigeAufgabe(Guid categoryId, string title = "Bankkonto")
        {
            var taskId = Guid.NewGuid();

            return new TaskItem
            {
                Id = taskId,
                TaskCategoryId = categoryId,
                Title = title,
                Description = "Schreibe eine Klasse Konto.",
                Difficulty = Difficulty.Medium,
                Order = 1,
                IsVisible = true,
                EvaluationMode = EvaluationMode.Both,
                Hints =
                [
                    new TaskHint { Id = Guid.NewGuid(), TaskItemId = taskId, Content = "Denk an den Konstruktor.", Order = 1 }
                ],
                Tests =
                [
                    new TaskTest
                    {
                        Id = Guid.NewGuid(),
                        TaskItemId = taskId,
                        Input = "100",
                        ExpectedOutput = "Stand: 100",
                        Description = "Das Programm gibt den Kontostand aus",
                        Order = 1
                    }
                ],
                UnitTestFiles =
                [
                    new TaskUnitTestFile
                    {
                        Id = Guid.NewGuid(),
                        TaskItemId = taskId,
                        FileName = "KontoTest.java",
                        Content = "class KontoTest {}",
                        Order = 1,
                        IsVisibleToParticipant = true
                    }
                ],
                ExpectedTypes =
                [
                    new TaskExpectedType
                    {
                        Id = Guid.NewGuid(),
                        TaskItemId = taskId,
                        Name = "Konto",
                        Order = 1,
                        Methods =
                        [
                            new TaskExpectedMethod
                            {
                                Id = Guid.NewGuid(),
                                Signature = "void einzahlen(int betrag)",
                                Name = "einzahlen",
                                Order = 1
                            }
                        ]
                    }
                ],
                CategoryWeights =
                [
                    new TaskCategoryWeight
                    {
                        Id = Guid.NewGuid(),
                        TaskItemId = taskId,
                        Category = EvaluationCategory.Functionality,
                        Weight = 70
                    }
                ]
            };
        }

        public static Submission Abgabe(Guid taskItemId, SubmissionStatus status = SubmissionStatus.Done)
        {
            var submissionId = Guid.NewGuid();

            return new Submission
            {
                Id = submissionId,
                TaskItemId = taskItemId,
                Status = status,
                SubmittedAt = DateTime.UtcNow,
                Files =
                [
                    new SubmissionFile
                    {
                        Id = Guid.NewGuid(),
                        SubmissionId = submissionId,
                        FileName = "Konto.java",
                        Content = "public class Konto {}"
                    }
                ]
            };
        }

        public static EvaluationResult Ergebnis(Guid submissionId)
        {
            var resultId = Guid.NewGuid();
            var categoryResultId = Guid.NewGuid();

            return new EvaluationResult
            {
                Id = resultId,
                SubmissionId = submissionId,
                TotalScore = 80,
                MaxScore = 100,
                CategoryResults =
                [
                    new CategoryResult
                    {
                        Id = categoryResultId,
                        EvaluationResultId = resultId,
                        Category = EvaluationCategory.Functionality,
                        Passed = false,
                        Points = 50,
                        MaxPoints = 70,
                        ErrorTip = "Prüfe die Ausgabe.",
                        TestCaseResults =
                        [
                            new TestCaseResult
                            {
                                Id = Guid.NewGuid(),
                                CategoryResultId = categoryResultId,
                                Description = "Das Programm gibt den Kontostand aus",
                                Input = "100",
                                ExpectedOutput = "Stand: 100",
                                ActualOutput = "Stand: 0",
                                Passed = false,
                                Order = 1
                            }
                        ]
                    }
                ]
            };
        }
    }
}
