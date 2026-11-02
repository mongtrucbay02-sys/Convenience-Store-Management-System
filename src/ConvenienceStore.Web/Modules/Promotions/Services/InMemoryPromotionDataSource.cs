namespace ConvenienceStore.Web.Modules.Promotions.Services;

public sealed class InMemoryPromotionDataSource : IPromotionDataSource
{
    private readonly object _gate = new();
    private readonly List<PromotionRecord> _records;
    private long _nextId;

    public InMemoryPromotionDataSource(IEnumerable<PromotionRecord> seed)
    {
        _records = seed.ToList();
        _nextId = _records.Count == 0 ? 1 : _records.Max(r => r.Id) + 1;
    }

    public Task<IReadOnlyList<PromotionRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PromotionRecord>>(_records.ToList());
        }
    }

    public Task<PromotionRecord?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_records.FirstOrDefault(r => r.Id == id));
        }
    }

    public Task<PromotionRecord> CreateAsync(PromotionRecord record, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var id = _nextId++;
            var saved = record with { Id = id, Code = $"KM-{id:0000}" };
            _records.Add(saved);
            return Task.FromResult(saved);
        }
    }

    public Task<bool> UpdateAsync(PromotionRecord record, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var index = _records.FindIndex(r => r.Id == record.Id);
            if (index < 0) return Task.FromResult(false);
            _records[index] = record;
            return Task.FromResult(true);
        }
    }
}
