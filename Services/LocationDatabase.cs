using SQLite;
using LocationTracker.Models;

namespace LocationTracker.Services;

public sealed class LocationDatabase
{
    private readonly SQLiteAsyncConnection _connection = new(
        Path.Combine(FileSystem.AppDataDirectory, "locationtracker.db3"));
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            await _connection.CreateTableAsync<LocationRecord>();
            _initialized = true;
        }
        finally { _initLock.Release(); }
    }

    public async Task AddAsync(LocationRecord record)
    {
        await InitializeAsync();
        await _connection.InsertAsync(record);
    }

    public async Task<List<LocationRecord>> GetAllAsync()
    {
        await InitializeAsync();
        return await _connection.Table<LocationRecord>()
            .OrderBy(x => x.TimestampUtcMs).ToListAsync();
    }

    public async Task ClearAsync()
    {
        await InitializeAsync();
        await _connection.DeleteAllAsync<LocationRecord>();
    }
}
