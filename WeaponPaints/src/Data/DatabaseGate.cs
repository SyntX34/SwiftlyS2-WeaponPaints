using System.Data;
using Microsoft.Extensions.Logging;

namespace WeaponPaints.Data;

public readonly record struct QueryArg(string Name, object? Value);

/*
  Thin wrapper around the SwiftlyS2 database service. The connection string
  and the driver come from configs/database.jsonc, so the plugin never holds
  credentials of its own. All the statements here are plain SQL and only use
  parameters, which keeps them working on MySQL, PostgreSQL and SQLite.
*/
public sealed class DatabaseGate
{
    private readonly Func<IDbConnection> _connect;
    private readonly ILogger _logger;

    public DatabaseGate(Func<IDbConnection> connect, ILogger logger)
    {
        _connect = connect;
        _logger = logger;
    }

    public bool Ready { get; private set; }

    public bool TryPing()
    {
        try
        {
            using IDbConnection connection = _connect();
            connection.Open();
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.ExecuteScalar();
            Ready = true;
            return true;
        }
        catch (Exception ex)
        {
            Ready = false;
            _logger.LogError("The WeaponPaints database did not answer: {Message}", ex.Message);
            return false;
        }
    }

    public List<T> Read<T>(string sql, Func<IDataReader, T> row, params QueryArg[] args)
    {
        List<T> list = new();
        try
        {
            using IDbConnection connection = _connect();
            connection.Open();
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            AddArgs(command, args);
            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(row(reader));
            }
        }
        catch (Exception ex)
        {
            // A WeaponPaints build without some table must not stop the other reads.
            _logger.LogDebug("{Statement} was skipped: {Message}", sql, ex.Message);
        }

        return list;
    }

    public void Each(string sql, Action<IDataReader> row, params QueryArg[] args)
    {
        Read(sql, reader =>
        {
            row(reader);
            return 0;
        }, args);
    }

    public int Execute(string sql, params QueryArg[] args)
    {
        try
        {
            using IDbConnection connection = _connect();
            connection.Open();
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            AddArgs(command, args);
            return command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogError("{Statement} failed: {Message}", sql, ex.Message);
            return -1;
        }
    }

    /*
    Rows are changed with an update first and an insert only when the update
    touched nothing. That keeps the same statement working on every driver
    instead of relying on ON DUPLICATE KEY UPDATE or ON CONFLICT.
    */
    public bool Upsert(string updateSql, string insertSql, params QueryArg[] args)
    {
        int updated = Execute(updateSql, args);
        if (updated != 0)
        {
            return updated > 0;
        }

        return Execute(insertSql, args) > 0;
    }

    private static void AddArgs(IDbCommand command, QueryArg[] args)
    {
        foreach (QueryArg arg in args)
        {
            IDbDataParameter parameter = command.CreateParameter();
            parameter.ParameterName = arg.Name;
            parameter.Value = arg.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
