using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MiniShop.Data;

public class SqliteUnicodeInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => Register(connection);

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
            sqlite.CreateFunction("lower", (string? s) => s?.ToLowerInvariant(), isDeterministic: true);
    }
}
