using Microsoft.Extensions.Logging;

namespace WeaponPaints.Data;

/*
  Runs resources/sql/schema.sql once on start. Every statement creates a table
  only when it is missing, so a server that already runs WeaponPaints keeps
  its data and a fresh database gets the tables it needs.
*/
public sealed class SchemaInstaller
{
    private readonly DatabaseGate _gate;
    private readonly ILogger _logger;

    public SchemaInstaller(DatabaseGate gate, ILogger logger)
    {
        _gate = gate;
        _logger = logger;
    }

    public void Run(string sqlFolder)
    {
        string path = Path.Combine(sqlFolder, "schema.sql");
        if (!File.Exists(path))
        {
            _logger.LogWarning("schema.sql was not found at {Path}. The tables have to exist already.", path);
            return;
        }

        int ran = 0;
        int failed = 0;
        foreach (string statement in ReadStatements(path))
        {
            if (_gate.Execute(statement) < 0)
            {
                failed++;
            }
            else
            {
                ran++;
            }
        }

        if (failed == 0)
        {
            _logger.LogInformation("WeaponPaints tables are ready ({Count} statements).", ran);
        }
        else
        {
            _logger.LogWarning("{Failed} of {Total} schema statements did not run. The database user may need create rights.", failed, ran + failed);
        }
    }

    /* The file is plain SQL. Comments are dropped and the rest is cut at each semicolon. */
    private static List<string> ReadStatements(string path)
    {
        string text = File.ReadAllText(path);
        while (true)
        {
            int open = text.IndexOf("/*", StringComparison.Ordinal);
            if (open < 0)
            {
                break;
            }

            int close = text.IndexOf("*/", open + 2, StringComparison.Ordinal);
            if (close < 0)
            {
                text = text[..open];
                break;
            }

            text = text[..open] + text[(close + 2)..];
        }

        List<string> statements = new();
        foreach (string part in text.Split(';'))
        {
            string statement = part.Trim();
            if (statement.Length > 0)
            {
                statements.Add(statement);
            }
        }

        return statements;
    }
}
