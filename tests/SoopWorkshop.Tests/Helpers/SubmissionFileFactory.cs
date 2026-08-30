using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Tests.Helpers
{
    // Erzeugt SubmissionFile-Instanzen für die Checker-Tests.
    // Die Navigation Submission bleibt bewusst ungesetzt, da die Checker
    // ausschließlich FileName und Content auswerten.
    public static class SubmissionFileFactory
    {
        public static SubmissionFile Create(string content, string fileName = "Main.java") =>
            new()
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                Content = content
            };

        // Mehrere Dateien mit durchnummerierten Namen: File1.java, File2.java, ...
        public static List<SubmissionFile> CreateMany(params string[] contents) =>
            contents
                .Select((content, index) => Create(content, $"File{index + 1}.java"))
                .ToList();
    }
}
