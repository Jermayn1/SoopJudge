using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Infrastructure.Evaluation;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation
{
    public class EvaluationWorkerTests
    {
        private readonly IEvaluationQueue _queue = Substitute.For<IEvaluationQueue>();
        private readonly ISubmissionRepository _submissionRepository = Substitute.For<ISubmissionRepository>();
        private readonly IEvaluationService _evaluationService = Substitute.For<IEvaluationService>();

        public EvaluationWorkerTests()
        {
            _queue.ReadAllAsync(Arg.Any<CancellationToken>()).Returns(EmptyQueue());
        }

        private EvaluationWorker CreateWorker()
        {
            var provider = Substitute.For<IServiceProvider>();
            provider.GetService(typeof(ISubmissionRepository)).Returns(_submissionRepository);
            provider.GetService(typeof(IEvaluationService)).Returns(_evaluationService);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(provider);

            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            return new EvaluationWorker(
                _queue,
                scopeFactory,
                Options.Create(new EvaluationOptions()),
                NullLogger<EvaluationWorker>.Instance);
        }

        private static async IAsyncEnumerable<Guid> EmptyQueue()
        {
            await Task.CompletedTask;
            yield break;
        }

        [Fact]
        public async Task ExecuteAsync_VerwaisteAbgabenVorhanden_MarkiertSieAlsFehlgeschlagen()
        {
            var verwaist = Guid.NewGuid();
            _submissionRepository
                .GetIdsByStatusAsync(Arg.Any<IReadOnlyList<SubmissionStatus>>(), Arg.Any<CancellationToken>())
                .Returns([verwaist]);

            var worker = CreateWorker();
            await worker.StartAsync(CancellationToken.None);
            await worker.ExecuteTask!;

            await _submissionRepository.Received(1).UpdateStatusAsync(
                verwaist,
                SubmissionStatus.Failed,
                Arg.Is<string>(message => message.Contains("Neustart")),
                Arg.Any<CancellationToken>());
        }

        // Eine Exception aus ExecuteAsync fährt sonst den kompletten Host herunter
        // (BackgroundServiceExceptionBehavior.StopHost) — die API würde bei einer
        // nicht erreichbaren Datenbank gar nicht erst starten.
        [Fact]
        public async Task ExecuteAsync_AufraeumenSchlaegtFehl_BeendetDenHostNicht()
        {
            _submissionRepository
                .GetIdsByStatusAsync(Arg.Any<IReadOnlyList<SubmissionStatus>>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Datenbank nicht erreichbar"));

            var worker = CreateWorker();
            await worker.StartAsync(CancellationToken.None);

            await Should.NotThrowAsync(async () => await worker.ExecuteTask!);
            worker.ExecuteTask!.IsFaulted.ShouldBeFalse();
        }
    }
}
