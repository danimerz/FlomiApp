using FlomiApp.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Tests;

/// <summary>
/// Erstellt einen frischen ApplicationDbContext auf einer offenen SQLite-
/// In-Memory-Verbindung. Die Verbindung muss offen gehalten werden, solange
/// der Context (und von ihm abgeleitete Contexts im selben Test) lebt —
/// sonst verschwindet die SQLite-In-Memory-DB sofort wieder.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public DbContextOptions<ApplicationDbContext> Options { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ApplicationDbContext(Options);
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext() => new(Options);

    public IDbContextFactory<ApplicationDbContext> CreateContextFactory() => new SimpleFactory(Options);

    public void Dispose() => _connection.Dispose();

    private sealed class SimpleFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public SimpleFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;
        public ApplicationDbContext CreateDbContext() => new(_options);
    }
}
