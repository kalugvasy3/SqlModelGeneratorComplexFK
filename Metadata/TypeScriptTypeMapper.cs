namespace SqlModelGenerator.Metadata;

// Types describe the default System.Text.Json wire representation, not CLR runtime objects.
public static class TypeScriptTypeMapper
{
    public static string ToType(string sqlTypeName, bool isNullable)
    {
        var type = sqlTypeName.ToLowerInvariant() switch
        {
            "bit" => "boolean",
            "bigint" or "int" or "smallint" or "tinyint" or "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "number",
            "char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" or "xml" or "uniqueidentifier" => "string",
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" or "time" => "string",
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => "string",
            "sql_variant" => "unknown",
            _ => throw new NotSupportedException($"SQL type '{sqlTypeName}' requires an explicit TypeScript mapping.")
        };
        return type + (isNullable ? " | null" : "");
    }

    public static string? Note(string sqlTypeName) => sqlTypeName.ToLowerInvariant() switch
    {
        "bigint" => "JSON number: integers outside JavaScript's safe integer range can lose precision. Use a string-valued API DTO for exact large IDs.",
        "decimal" or "numeric" or "money" or "smallmoney" => "JSON number: JavaScript cannot represent all decimal values exactly. Use a string-valued API DTO when exact decimal precision is required.",
        "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "JSON date/time string; not a JavaScript Date instance.",
        "time" => "JSON TimeSpan string, using the backend's TimeSpan serialization format.",
        "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => "Base64-encoded JSON string (System.Text.Json byte[] representation).",
        _ => null
    };
}
