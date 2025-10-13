using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace NewsFeedBackend.Services;

public sealed class SemanticReranker
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _gen;
    private readonly IMemoryCache _cache;

    public SemanticReranker(IEmbeddingGenerator<string, Embedding<float>> gen, IMemoryCache? cache = null)
    {
        _gen = gen;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
    }
    public async Task<IReadOnlyList<(T item, double score)>> RerankAsync<T>(
        string intent,
        IReadOnlyList<T> candidates,
        Func<T, string> textSelector,
        int? topK = null,
        double? minScore = null,
        CancellationToken ct = default)
    {
        if (candidates.Count == 0) return Array.Empty<(T, double)>();

        var intentVec = await GetOrCreateVectorAsync(
            "__intent__:" + intent,
            () => _gen.GenerateVectorAsync(intent, cancellationToken: ct),
            ct
        );

        var tasks = candidates.Select(async c =>
        {
            var text = textSelector(c) ?? string.Empty;
            var key = "cand:" + text;
            var vec = await GetOrCreateVectorAsync(
                key,
                () => _gen.GenerateVectorAsync(text, cancellationToken: ct),
                ct
            );
            var score = Cosine(intentVec.Span, vec.Span);
            return (c, score);
        });

        var scored = await Task.WhenAll(tasks);

        var filtered = (minScore.HasValue ? scored.Where(x => x.score >= minScore.Value) : scored)
                       .OrderByDescending(x => x.score);

        return (topK.HasValue ? filtered.Take(topK.Value) : filtered).ToList();
    }

    public async Task<IReadOnlyList<T>> RerankItemsAsync<T>(
        string intent,
        IReadOnlyList<T> candidates,
        Func<T, string> textSelector,
        int? topK = null,
        double? minScore = null,
        CancellationToken ct = default)
    {
        var ranked = await RerankAsync(intent, candidates, textSelector, topK, minScore, ct);
        return ranked.Select(x => x.item).ToList();
    }
    private async Task<ReadOnlyMemory<float>> GetOrCreateVectorAsync(
        string key,
        Func<Task<ReadOnlyMemory<float>>> factory,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(key, out ReadOnlyMemory<float> cached))
            return cached;

        var vec = await factory();
        _cache.Set(key, vec, TimeSpan.FromMinutes(5));
        return vec;
    }

    static double Cosine(ReadOnlySpan<float> u, ReadOnlySpan<float> v)
    {
        double dot = 0, nu = 0, nv = 0;
        var n = Math.Min(u.Length, v.Length);
        for (int i = 0; i < n; i++) { dot += u[i] * v[i]; nu += u[i] * u[i]; nv += v[i] * v[i]; }
        return dot / (Math.Sqrt(nu) * Math.Sqrt(nv) + 1e-9);
    }
}
