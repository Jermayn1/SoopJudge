using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation
{
    public class JavaAnalyzerTests
    {
        private readonly EvaluationOptions _options = new();

        private JavaAnalyzer CreateAnalyzer(params IEvaluationChecker[] checkers) =>
            new(checkers, Options.Create(_options), NullLogger<JavaAnalyzer>.Instance);

        // Checker-Attrappe: liefert die übergebenen Teilprüfungen zurück.
        private static IEvaluationChecker Checker(
            EvaluationCategory category,
            int order,
            bool applicable = true,
            string? errorTip = null,
            params bool[] passed)
        {
            var checker = Substitute.For<IEvaluationChecker>();
            checker.Category.Returns(category);
            checker.Order.Returns(order);
            checker.IsApplicable(Arg.Any<EvaluationContext>()).Returns(applicable);

            var results = passed.Length == 0 ? [true] : passed;

            checker.CheckAsync(Arg.Any<EvaluationContext>(), Arg.Any<CancellationToken>())
                .Returns(new CheckerOutcome(
                    [.. results.Select((value, index) => new TestCaseResult
                    {
                        Id = Guid.NewGuid(),
                        Description = $"{category} {index}",
                        Passed = value
                    })],
                    errorTip));

            return checker;
        }

        private static Submission CreateSubmission(TaskItem? task = null)
        {
            var taskItem = task ?? new TaskItem { Id = Guid.NewGuid() };

            return new Submission
            {
                Id = Guid.NewGuid(),
                TaskItemId = taskItem.Id,
                Task = taskItem,
                Files = [new SubmissionFile { Id = Guid.NewGuid(), FileName = "Main.java", Content = "class Main { }" }]
            };
        }

        // Clean Code entsteht aus mehreren unabhängigen Checkern - sie müssen zu
        // einer Kategorie mit allen Teilprüfungen zusammenfließen.
        [Fact]
        public async Task AnalyzeAsync_MehrereCheckerEinerKategorie_ErgebenEineKategorie()
        {
            var analyzer = CreateAnalyzer(
                Checker(EvaluationCategory.CleanCode, 20, passed: [true]),
                Checker(EvaluationCategory.CleanCode, 30, passed: [true, false]),
                Checker(EvaluationCategory.Compilability, 10));

            var result = await analyzer.AnalyzeAsync(CreateSubmission(), CancellationToken.None);

            var cleanCode = result.CategoryResults.Single(c => c.Category == EvaluationCategory.CleanCode);
            cleanCode.TestCaseResults.Count.ShouldBe(3);
            cleanCode.Passed.ShouldBeFalse();
        }

        [Fact]
        public async Task AnalyzeAsync_MehrereHinweiseEinerKategorie_GehenNichtVerloren()
        {
            var analyzer = CreateAnalyzer(
                Checker(EvaluationCategory.CleanCode, 20, errorTip: "Keine Umlaute.", passed: [false]),
                Checker(EvaluationCategory.CleanCode, 30, errorTip: "PascalCase nutzen.", passed: [false]),
                Checker(EvaluationCategory.Compilability, 10));

            var result = await analyzer.AnalyzeAsync(CreateSubmission(), CancellationToken.None);

            var cleanCode = result.CategoryResults.Single(c => c.Category == EvaluationCategory.CleanCode);
            cleanCode.ErrorTip.ShouldContain("Keine Umlaute.");
            cleanCode.ErrorTip.ShouldContain("PascalCase nutzen.");
        }

        [Fact]
        public async Task AnalyzeAsync_NichtAnwendbarerChecker_TauchtNichtInDerWertungAuf()
        {
            var nichtAnwendbar = Checker(EvaluationCategory.Functionality, 40, applicable: false);

            var analyzer = CreateAnalyzer(
                Checker(EvaluationCategory.CleanCode, 20),
                Checker(EvaluationCategory.Compilability, 10),
                nichtAnwendbar);

            var result = await analyzer.AnalyzeAsync(CreateSubmission(), CancellationToken.None);

            result.CategoryResults.ShouldNotContain(c => c.Category == EvaluationCategory.Functionality);
            await nichtAnwendbar.DidNotReceive().CheckAsync(Arg.Any<EvaluationContext>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AnalyzeAsync_CheckerLaufenInDerReihenfolgeVonOrder()
        {
            var zuerst = Checker(EvaluationCategory.Compilability, 10);
            var danach = Checker(EvaluationCategory.Functionality, 40);

            // Bewusst in falscher Reihenfolge übergeben.
            var analyzer = CreateAnalyzer(danach, zuerst);

            await analyzer.AnalyzeAsync(CreateSubmission(), CancellationToken.None);

            Received.InOrder(() =>
            {
                zuerst.CheckAsync(Arg.Any<EvaluationContext>(), Arg.Any<CancellationToken>());
                danach.CheckAsync(Arg.Any<EvaluationContext>(), Arg.Any<CancellationToken>());
            });
        }

        [Fact]
        public async Task AnalyzeAsync_AufgabenspezifischesGewicht_SchlaegtDenStandard()
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                CategoryWeights =
                [
                    new TaskCategoryWeight { Id = Guid.NewGuid(), Category = EvaluationCategory.CleanCode, Weight = 50 }
                ]
            };

            var analyzer = CreateAnalyzer(
                Checker(EvaluationCategory.CleanCode, 20),
                Checker(EvaluationCategory.Compilability, 10));

            var result = await analyzer.AnalyzeAsync(CreateSubmission(task), CancellationToken.None);

            // 50 zu 20 statt der Standardgewichte 15 zu 20.
            result.CategoryResults.Single(c => c.Category == EvaluationCategory.CleanCode).MaxPoints.ShouldBe(71);
            result.CategoryResults.Single(c => c.Category == EvaluationCategory.Compilability).MaxPoints.ShouldBe(29);
        }

        [Fact]
        public async Task AnalyzeAsync_FehlendesGewicht_WirftMitHinweisAufDieKonfiguration()
        {
            _options.CategoryWeights = new Dictionary<EvaluationCategory, double>
            {
                [EvaluationCategory.Compilability] = 20
            };

            var analyzer = CreateAnalyzer(
                Checker(EvaluationCategory.Compilability, 10),
                Checker(EvaluationCategory.UnitTests, 50));

            var exception = await Should.ThrowAsync<InvalidOperationException>(
                () => analyzer.AnalyzeAsync(CreateSubmission(), CancellationToken.None));

            exception.Message.ShouldContain("Evaluation:CategoryWeights");
        }

        [Fact]
        public async Task AnalyzeAsync_VerknuepftErgebnisMitAbgabeUndKategorien()
        {
            var submission = CreateSubmission();

            var analyzer = CreateAnalyzer(
                Checker(EvaluationCategory.CleanCode, 20),
                Checker(EvaluationCategory.Compilability, 10));

            var result = await analyzer.AnalyzeAsync(submission, CancellationToken.None);

            result.SubmissionId.ShouldBe(submission.Id);
            result.MaxScore.ShouldBe(100);
            result.CategoryResults.ShouldAllBe(c => c.EvaluationResultId == result.Id);
        }

        // Das Arbeitsverzeichnis gehört dem Analyzer - bleibt es liegen, läuft
        // die Platte auf Dauer voll.
        [Fact]
        public async Task AnalyzeAsync_RaeumtDasArbeitsverzeichnisAuf()
        {
            string? workingDirectory = null;

            var checker = Substitute.For<IEvaluationChecker>();
            checker.Category.Returns(EvaluationCategory.Compilability);
            checker.Order.Returns(10);
            checker.IsApplicable(Arg.Any<EvaluationContext>()).Returns(true);
            checker.CheckAsync(Arg.Any<EvaluationContext>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    workingDirectory = callInfo.Arg<EvaluationContext>().WorkingDirectory;
                    Directory.Exists(workingDirectory).ShouldBeTrue();

                    return Task.FromResult(new CheckerOutcome(
                        [new TestCaseResult { Id = Guid.NewGuid(), Description = "egal", Passed = true }],
                        null));
                });

            await CreateAnalyzer(checker).AnalyzeAsync(CreateSubmission(), CancellationToken.None);

            workingDirectory.ShouldNotBeNull();
            Directory.Exists(workingDirectory).ShouldBeFalse();
        }

        [Fact]
        public async Task AnalyzeAsync_CheckerWirft_RaeumtTrotzdemAuf()
        {
            string? workingDirectory = null;

            var checker = Substitute.For<IEvaluationChecker>();
            checker.Category.Returns(EvaluationCategory.Compilability);
            checker.Order.Returns(10);
            checker.IsApplicable(Arg.Any<EvaluationContext>()).Returns(true);
            checker.CheckAsync(Arg.Any<EvaluationContext>(), Arg.Any<CancellationToken>())
                .Returns<Task<CheckerOutcome>>(callInfo =>
                {
                    workingDirectory = callInfo.Arg<EvaluationContext>().WorkingDirectory;
                    throw new IOException("kaputt");
                });

            await Should.ThrowAsync<IOException>(
                () => CreateAnalyzer(checker).AnalyzeAsync(CreateSubmission(), CancellationToken.None));

            workingDirectory.ShouldNotBeNull();
            Directory.Exists(workingDirectory).ShouldBeFalse();
        }
    }
}
