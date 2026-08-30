using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Tests.Helpers
{
    // Baut den EvaluationContext, den jeder Checker erwartet. Ohne den Helfer
    // wiederholt sich in jedem Checker-Test dasselbe Gerüst aus Abgabe, Aufgabe
    // und Arbeitsverzeichnis.
    public static class EvaluationContextFactory
    {
        public static EvaluationContext For(
            TaskItem? task = null,
            IReadOnlyList<SubmissionFile>? files = null,
            CompilationResult? compilation = null,
            string workingDirectory = "/tmp/egal")
        {
            var taskItem = task ?? new TaskItem { Id = Guid.NewGuid() };

            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                TaskItemId = taskItem.Id,
                Task = taskItem
            };

            return new EvaluationContext
            {
                Submission = submission,
                Task = taskItem,
                WorkingDirectory = workingDirectory,
                Files = files ?? [],
                Compilation = compilation
            };
        }

        public static TaskItem TaskWithTests(params TaskTest[] tests) =>
            new() { Id = Guid.NewGuid(), Tests = tests };
    }
}
