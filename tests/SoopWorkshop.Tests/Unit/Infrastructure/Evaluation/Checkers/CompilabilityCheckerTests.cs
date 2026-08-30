using Microsoft.Extensions.Options;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class CompilabilityCheckerTests : IDisposable
    {
        private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
        private readonly EvaluationOptions _options = new() { CompileTimeoutSeconds = 30 };
        private readonly string _workingDirectory;

        public CompilabilityCheckerTests()
        {
            _workingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workingDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_workingDirectory))
                Directory.Delete(_workingDirectory, recursive: true);

            GC.SuppressFinalize(this);
        }

        private CompilabilityChecker CreateChecker() => new(_processRunner, Options.Create(_options));

        private EvaluationContext Context(IReadOnlyList<SubmissionFile> files) =>
            EvaluationContextFactory.For(files: files, workingDirectory: _workingDirectory);

        private void JavacReturns(ProcessResult result) =>
            _processRunner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>()).Returns(result);

        [Fact]
        public async Task CheckAsync_KompilierungErfolgreich_HinterlegtHauptklasseImKontext()
        {
            JavacReturns(ProcessResultFactory.Success());

            var context = Context([SubmissionFileFactory.Create("public class Main { public static void main(String[] a) {} }")]);

            var outcome = await CreateChecker().CheckAsync(context, CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
            outcome.ErrorTip.ShouldBeNull();

            context.Compilation.ShouldNotBeNull();
            context.Compilation.Success.ShouldBeTrue();
            context.Compilation.MainClassName.ShouldBe("Main");
            context.Compilation.WorkingDirectory.ShouldBe(_workingDirectory);
        }

        [Fact]
        public async Task CheckAsync_KompilierungFehlgeschlagen_LiefertDieCompilerausgabe()
        {
            JavacReturns(ProcessResultFactory.Failure("Main.java:3: error: ';' expected"));

            var context = Context([SubmissionFileFactory.Create("public class Main { kaputt }")]);

            var outcome = await CreateChecker().CheckAsync(context, CancellationToken.None);

            var result = outcome.Results.ShouldHaveSingleItem();
            result.Passed.ShouldBeFalse();
            result.ActualOutput.ShouldContain("';' expected");
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();

            context.Compilation!.MainClassName.ShouldBeNull();
        }

        [Fact]
        public async Task CheckAsync_JavacFehlt_WeistAufDasFehlendeJdkHin()
        {
            JavacReturns(ProcessResultFactory.NotFound());

            var outcome = await CreateChecker().CheckAsync(
                Context([SubmissionFileFactory.Create("public class Main {}")]),
                CancellationToken.None);

            var result = outcome.Results.ShouldHaveSingleItem();
            result.Passed.ShouldBeFalse();
            result.ActualOutput.ShouldContain("JDK");
        }

        [Fact]
        public async Task CheckAsync_Zeitueberschreitung_NenntDieZeitgrenze()
        {
            JavacReturns(ProcessResultFactory.TimedOut());

            var outcome = await CreateChecker().CheckAsync(
                Context([SubmissionFileFactory.Create("public class Main {}")]),
                CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().ActualOutput.ShouldContain("30 Sekunden");
        }

        [Fact]
        public async Task CheckAsync_MehrereDateien_UebergibtAlleAnJavac()
        {
            JavacReturns(ProcessResultFactory.Success());

            var files = SubmissionFileFactory.CreateMany("public class File1 {}", "public class File2 {}");

            await CreateChecker().CheckAsync(Context(files), CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.FileName == "javac"
                                            && r.Arguments.Count(a => a.EndsWith(".java")) == 2),
                Arg.Any<CancellationToken>());

            File.Exists(Path.Combine(_workingDirectory, "File1.java")).ShouldBeTrue();
            File.Exists(Path.Combine(_workingDirectory, "File2.java")).ShouldBeTrue();
        }

        // javac stellt den übergebenen Pfad seinen Fehlermeldungen voran. Mit
        // absoluten Pfaden las der Teilnehmer das Temp-Verzeichnis des Servers.
        [Fact]
        public async Task CheckAsync_UebergibtJavacNurDateinamenOhnePfad()
        {
            JavacReturns(ProcessResultFactory.Success());

            await CreateChecker().CheckAsync(
                Context([SubmissionFileFactory.Create("public class Main {}")]),
                CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.Arguments.Contains("Main.java")
                                            && r.Arguments.All(a => !a.Contains(_workingDirectory))
                                            && r.WorkingDirectory == _workingDirectory),
                Arg.Any<CancellationToken>());
        }

        // Zweite Verteidigungslinie hinter der Upload-Prüfung: ein Dateiname mit
        // Pfadanteilen darf nicht außerhalb des Arbeitsverzeichnisses landen.
        [Theory]
        [InlineData("../Main.java")]
        [InlineData("..\\Main.java")]
        [InlineData("unterordner/Main.java")]
        public async Task CheckAsync_DateinameEnthaeltPfad_SchreibtNurInDasArbeitsverzeichnis(string fileName)
        {
            JavacReturns(ProcessResultFactory.Success());

            await CreateChecker().CheckAsync(
                Context([SubmissionFileFactory.Create("public class Main {}", fileName)]),
                CancellationToken.None);

            Directory.GetFiles(_workingDirectory).ShouldHaveSingleItem()
                .ShouldBe(Path.Combine(_workingDirectory, "Main.java"));
        }

        [Fact]
        public void IsApplicable_ImmerAnwendbar()
        {
            CreateChecker().IsApplicable(Context([])).ShouldBeTrue();
        }
    }
}
