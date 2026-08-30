using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Helpers
{
    // Baut gültige Transferdateien, an denen ein Test dann genau eine Sache
    // kaputt macht. Ohne das steht in jedem Test dieselbe halbe Seite Aufbau,
    // und man sieht nicht mehr, worum es geht.
    public static class TaskBundleFactory
    {
        public static TaskBundleUnitTestFileDto JUnitFile(string fileName = "MainTest.java") => new()
        {
            FileName = fileName,
            Content = "class MainTest { }",
            Order = 1,
            IsVisibleToParticipant = false
        };

        public static TaskBundleTestDto ConsoleTest(string description = "Das Programm gibt den Gruss aus") => new()
        {
            Input = string.Empty,
            ExpectedOutput = "Hallo Soop",
            Description = description,
            Order = 1
        };

        public static TaskBundleTaskDto Task(
            Guid? id = null,
            string title = "Hallo Soop",
            EvaluationMode mode = EvaluationMode.ConsoleOnly,
            bool isVisible = false) => new()
            {
                Id = id ?? Guid.NewGuid(),
                Title = title,
                Description = "Gib den Gruss aus.",
                Difficulty = Difficulty.Easy,
                Order = 1,
                IsVisible = isVisible,
                EvaluationMode = mode,
                Tests = mode is EvaluationMode.ConsoleOnly or EvaluationMode.Both ? [ConsoleTest()] : [],
                UnitTestFiles = mode is EvaluationMode.UnitTestOnly or EvaluationMode.Both ? [JUnitFile()] : []
            };

        public static TaskBundleCategoryDto Category(
            Guid? id = null,
            string name = "Grundlagen",
            params TaskBundleTaskDto[] tasks) => new()
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Order = 1,
                IsVisible = true,
                IconName = "Terminal",
                Tasks = [.. tasks]
            };

        public static TaskBundleDto Bundle(params TaskBundleCategoryDto[] categories) => new()
        {
            FormatVersion = TaskBundleFormat.CurrentVersion,
            ExportedAt = DateTimeOffset.UtcNow,
            Categories = [.. categories]
        };
    }
}
