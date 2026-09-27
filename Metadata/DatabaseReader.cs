using System.Data.Common;
using Microsoft.Data.SqlClient;
using SqlModelGenerator.Metadata.Models;

namespace SqlModelGenerator.Metadata;

public sealed class DatabaseReader
{
    private readonly string _connectionString;
    public DatabaseReader(string connectionString) => _connectionString = connectionString;

    public async Task<DatabaseModel> ReadAsync(CancellationToken cancellationToken = default)
    {
        var model = new DatabaseModel();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        model.Tables.AddRange(await ReadObjectsAsync<DbTableInfo>(connection, "U", cancellationToken));
        model.Views.AddRange(await ReadObjectsAsync<DbViewInfo>(connection, "V", cancellationToken));
        await ReadKeysAsync(connection, model, cancellationToken);
        await ReadForeignKeysAsync(connection, model, cancellationToken);
        await ReadProceduresAsync(connection, model, cancellationToken);
        return model;
    }

    private static async Task<List<T>> ReadObjectsAsync<T>(SqlConnection connection, string objectType, CancellationToken token) where T : DbObjectBase, new()
    {
        const string sql = """
            SELECT o.object_id, s.name, o.name, c.column_id, c.name,
                   TYPE_NAME(c.system_type_id), c.max_length, c.precision, c.scale,
                   c.is_nullable, c.is_identity, c.is_computed,
                   dc.definition, cc.definition, cc.is_persisted
            FROM sys.objects o
            JOIN sys.schemas s ON s.schema_id = o.schema_id
            JOIN sys.columns c ON c.object_id = o.object_id
            LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
            LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
            WHERE o.type = @objectType AND o.is_ms_shipped = 0
            ORDER BY o.object_id, c.column_id;
            """;
        var map = new Dictionary<int, T>();
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@objectType", objectType);
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var id = reader.GetInt32(0);
            if (!map.TryGetValue(id, out var obj))
            {
                obj = new T { ObjectId = id, Schema = reader.GetString(1), Name = reader.GetString(2) };
                map.Add(id, obj);
            }
            var type = reader.IsDBNull(5) ? "CLR/unknown" : reader.GetString(5);
            var nullable = reader.GetBoolean(9);
            string clr;
            try { clr = SqlTypeMapper.ToClrType(type, nullable); }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException($"Column [{obj.Schema}].[{obj.Name}].[{reader.GetString(4)}]: {ex.Message}", ex);
            }
            obj.Columns.Add(new DbColumnInfo
            {
                Ordinal = reader.GetInt32(3), Name = reader.GetString(4), SqlTypeName = type,
                ClrTypeName = clr, MaxLength = reader.GetInt16(6), Precision = reader.GetByte(7), Scale = reader.GetByte(8),
                IsNullable = nullable, IsIdentity = reader.GetBoolean(10), IsComputed = reader.GetBoolean(11),
                DefaultSql = reader.IsDBNull(12) ? null : reader.GetString(12),
                ComputedSql = reader.IsDBNull(13) ? null : reader.GetString(13),
                IsStored = !reader.IsDBNull(14) && reader.GetBoolean(14)
            });
        }
        return map.Values.OrderBy(x => x.Schema, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToList();
    }

    private static async Task ReadKeysAsync(SqlConnection connection, DatabaseModel model, CancellationToken token)
    {
        const string sql = """
            SELECT i.object_id, i.index_id, i.name, i.is_primary_key, i.has_filter, c.name
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.is_unique = 1 AND i.is_disabled = 0 AND i.is_hypothetical = 0 AND ic.key_ordinal > 0
            ORDER BY i.object_id, i.index_id, ic.key_ordinal;
            """;
        var tables = model.Tables.ToDictionary(t => t.ObjectId);
        var keys = new Dictionary<(int, int), DbUniqueKeyInfo>();
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var id = reader.GetInt32(0);
            if (!tables.TryGetValue(id, out var table)) continue;
            var keyId = (id, reader.GetInt32(1));
            if (!keys.TryGetValue(keyId, out var key))
            {
                key = new DbUniqueKeyInfo { Name = reader.GetString(2), IsFiltered = reader.GetBoolean(4) };
                keys.Add(keyId, key);
                table.UniqueKeys.Add(key);
            }
            var column = reader.GetString(5);
            key.Columns.Add(column);
            if (reader.GetBoolean(3)) table.PrimaryKeyColumns.Add(column);
        }
    }

    private static async Task ReadForeignKeysAsync(SqlConnection connection, DatabaseModel model, CancellationToken token)
    {
        const string sql = """
            SELECT fk.object_id, fk.name, fk.parent_object_id, fk.referenced_object_id,
                   pc.name, rc.name, fk.delete_referential_action_desc, fk.update_referential_action_desc,
                   fk.is_disabled, fk.is_not_trusted
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
            JOIN sys.columns pc ON pc.object_id = fc.parent_object_id AND pc.column_id = fc.parent_column_id
            JOIN sys.columns rc ON rc.object_id = fc.referenced_object_id AND rc.column_id = fc.referenced_column_id
            ORDER BY fk.object_id, fc.constraint_column_id;
            """;
        var tables = model.Tables.ToDictionary(t => t.ObjectId);
        var keys = new Dictionary<int, DbForeignKeyInfo>();
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            if (!tables.TryGetValue(reader.GetInt32(2), out var dependent) || !tables.TryGetValue(reader.GetInt32(3), out var principal))
            {
                var warning = $"FK {reader.GetString(1)} skipped: endpoint metadata is not visible. Check VIEW DEFINITION permissions.";
                if (!model.Warnings.Contains(warning)) model.Warnings.Add(warning);
                continue;
            }
            var id = reader.GetInt32(0);
            if (!keys.TryGetValue(id, out var fk))
            {
                fk = new DbForeignKeyInfo
                {
                    Name = reader.GetString(1), DependentTable = dependent, PrincipalTable = principal,
                    DeleteAction = reader.GetString(6), UpdateAction = reader.GetString(7),
                    IsDisabled = reader.GetBoolean(8), IsNotTrusted = reader.GetBoolean(9)
                };
                keys.Add(id, fk);
                model.ForeignKeys.Add(fk);
            }
            fk.Columns.Add(new DbForeignKeyColumn(reader.GetString(4), reader.GetString(5)));
        }
    }

    private static async Task ReadProceduresAsync(SqlConnection connection, DatabaseModel model, CancellationToken token)
    {
        const string sql = """
            SELECT p.object_id, s.name, p.name FROM sys.procedures p
            JOIN sys.schemas s ON s.schema_id = p.schema_id
            WHERE p.is_ms_shipped = 0 ORDER BY s.name, p.name;
            """;
        await using (var cmd = new SqlCommand(sql, connection))
        await using (var reader = await cmd.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                model.Procedures.Add(new DbProcedureInfo { ObjectId = reader.GetInt32(0), Schema = reader.GetString(1), Name = reader.GetString(2) });

        foreach (var proc in model.Procedures)
        {
            try
            {
                await ReadParametersAsync(connection, proc, token);
                await using var cmd = new SqlCommand("""
                    SELECT is_hidden, column_ordinal, name, is_nullable, system_type_name, error_message
                    FROM sys.dm_exec_describe_first_result_set_for_object(@objectId, 0)
                    ORDER BY column_ordinal;
                    """, connection);
                cmd.Parameters.AddWithValue("@objectId", proc.ObjectId);
                await using var reader = await cmd.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token)) ReadResultColumn(reader, proc);
            }
            catch (Exception ex) when (ex is SqlException or NotSupportedException)
            {
                proc.GenerationError = ex.Message;
                proc.ResultColumns.Clear();
            }
        }
    }

    // Named columns avoid depending on the catalog function's physical column order.
    public static void ReadResultColumn(DbDataReader reader, DbProcedureInfo proc)
    {
        if (reader["error_message"] is string error) throw new NotSupportedException(error);
        if (reader["is_hidden"] is true) return;
        if (reader["column_ordinal"] is DBNull) return;
        if (reader["name"] is not string name) throw new NotSupportedException("Result column has no name; add a SQL alias.");
        if (reader["system_type_name"] is not string fullType) throw new NotSupportedException("CLR result type needs explicit mapping.");
        var type = fullType.Split('(', ' ')[0].ToLowerInvariant();
        var nullable = reader["is_nullable"] is not false;
        proc.ResultColumns.Add(new DbResultColumnInfo
        {
            Ordinal = (int)reader["column_ordinal"], Name = name, SqlTypeName = type,
            IsNullable = nullable, ClrTypeName = SqlTypeMapper.ToClrType(type, nullable)
        });
    }

    private static async Task ReadParametersAsync(SqlConnection connection, DbProcedureInfo proc, CancellationToken token)
    {
        const string sql = """
            SELECT p.parameter_id, p.name, TYPE_NAME(p.system_type_id), p.max_length,
                   p.precision, p.scale, p.is_output, p.has_default_value, p.is_nullable
            FROM sys.parameters p WHERE p.object_id = @objectId AND p.parameter_id > 0
            ORDER BY p.parameter_id;
            """;
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@objectId", proc.ObjectId);
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var type = reader.IsDBNull(2) ? "CLR/unknown" : reader.GetString(2);
            var nullable = reader.GetBoolean(8);
            proc.Parameters.Add(new DbParameterInfo
            {
                Ordinal = reader.GetInt32(0), Name = reader.GetString(1), SqlTypeName = type,
                ClrTypeName = SqlTypeMapper.ToClrType(type, nullable), MaxLength = reader.GetInt16(3),
                Precision = reader.GetByte(4), Scale = reader.GetByte(5), IsOutput = reader.GetBoolean(6),
                HasDefaultValue = reader.GetBoolean(7), IsNullable = nullable
            });
        }
    }
}
