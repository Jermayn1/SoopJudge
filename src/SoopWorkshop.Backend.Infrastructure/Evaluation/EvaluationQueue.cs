using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation
{
    // Begrenzte Warteschlange der ausstehenden Auswertungen.
    // Begrenzt, weil eine unbegrenzte Warteschlange bei einem Ansturm nur den
    // Speicher füllt, ohne dass irgendjemand früher ein Ergebnis bekäme.
    public class EvaluationQueue : IEvaluationQueue
    {
        private readonly Channel<Guid> _channel;

        public EvaluationQueue(IOptions<EvaluationOptions> options)
        {
            _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(options.Value.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
        }

        public ValueTask EnqueueAsync(Guid submissionId, CancellationToken cancellationToken) =>
            _channel.Writer.WriteAsync(submissionId, cancellationToken);

        public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
