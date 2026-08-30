namespace SoopWorkshop.Backend.Application.Evaluation.Interfaces
{
    // Warteschlange zwischen Abgabe und Auswertung. Entkoppelt den HTTP-Request
    // von der eigentlichen Arbeit und begrenzt, wie viel gleichzeitig läuft.
    public interface IEvaluationQueue
    {
        // Wartet, wenn die Warteschlange voll ist, statt unbegrenzt Arbeit anzusammeln.
        ValueTask EnqueueAsync(Guid submissionId, CancellationToken cancellationToken);

        IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
    }
}
