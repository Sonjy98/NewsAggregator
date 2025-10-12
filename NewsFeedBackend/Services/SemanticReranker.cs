using Microsoft.Extensions.AI;

namespace NewsFeedBackend.Services;

public sealed class SemanticReranker
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _gen;

    public SemanticReranker(IEmbeddingGenerator<string, Embedding<float>> gen) => _gen = gen;

    public async Task<IReadOnlyList<(T item, double score)>> RerankAsync<T>(
        string intent,
        IReadOnlyList<T> candidates,
        Func<T, string> textSelector,
        CancellationToken ct = default)
    {
        // Single-value helper: returns ReadOnlyMemory<float>
        var intentVec = await _gen.GenerateVectorAsync(intent, cancellationToken: ct);

        var results = new List<(T, double)>(candidates.Count);
        foreach (var c in candidates)
        {
            var vec = await _gen.GenerateVectorAsync(textSelector(c), cancellationToken: ct);
            var score = Cosine(intentVec.Span, vec.Span);
            results.Add((c, score));
        }
        return results.OrderByDescending(x => x.Item2).ToList();
    }

    static double Cosine(ReadOnlySpan<float> u, ReadOnlySpan<float> v)
    {
        double dot = 0, nu = 0, nv = 0;
        var n = Math.Min(u.Length, v.Length);
        for (int i = 0; i < n; i++) { dot += u[i] * v[i]; nu += u[i] * u[i]; nv += v[i] * v[i]; }
        return dot / (Math.Sqrt(nu) * Math.Sqrt(nv) + 1e-9);
    }
}
