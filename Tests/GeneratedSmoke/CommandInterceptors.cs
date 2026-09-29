using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

// No SQL server is needed or contacted. All connection opens and command executions are suppressed.
sealed class NeverConnect : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        => InterceptionResult.Suppress();
    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(InterceptionResult.Suppress());
}

sealed class CapturedCommands : DbCommandInterceptor
{
    public object?[] InputValues { get; private set; } = [];
    public int CommandCount { get; private set; }
    public string Text { get; private set; } = "";
    public int[] Sizes { get; private set; } = [];
    public CancellationToken Token { get; private set; }
    private void Capture(DbCommand command, CancellationToken token)
    {
        CommandCount++;
        InputValues = command.Parameters.Cast<DbParameter>().Select(p => p.Value).ToArray();
        Text = command.CommandText;
        Sizes = command.Parameters.Cast<DbParameter>().Select(p => p.Size).ToArray();
        Token = token;
        foreach (DbParameter p in command.Parameters)
            if (p.Direction == ParameterDirection.InputOutput) p.Value = p.DbType == DbType.Int32 ? 42 : DBNull.Value;
    }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(command, cancellationToken);
        return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(3));
    }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Capture(command, cancellationToken);
        var data = new DataTable();
        data.Columns.Add("Value", typeof(int));
        data.Rows.Add(123);
        return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(data.CreateDataReader()));
    }
}
