namespace SqlModelGenerator.Metadata;

public static class SqlTypeMapper
{
    public static string ToClrType(string sqlTypeName, bool isNullable)
    {
        var sql = sqlTypeName.ToLowerInvariant();

        var baseType = sql switch
        {
            "bigint" => "long",
            "binary" => "byte[]",
            "bit" => "bool",
            "char" => "string",
            "date" => "global::System.DateTime",
            "datetime" => "global::System.DateTime",
            "datetime2" => "global::System.DateTime",
            "datetimeoffset" => "global::System.DateTimeOffset",
            "decimal" => "decimal",
            "float" => "double",
            "image" => "byte[]",
            "int" => "int",
            "money" => "decimal",
            "nchar" => "string",
            "ntext" => "string",
            "numeric" => "decimal",
            "nvarchar" => "string",
            "real" => "float",
            "smalldatetime" => "global::System.DateTime",
            "smallint" => "short",
            "smallmoney" => "decimal",
            "sql_variant" => "object",
            "text" => "string",
            "time" => "global::System.TimeSpan",
            "timestamp" or "rowversion" => "byte[]",
            "tinyint" => "byte",
            "uniqueidentifier" => "global::System.Guid",
            "varbinary" => "byte[]",
            "varchar" => "string",
            "xml" => "string",
            _ => throw new NotSupportedException($"SQL type '{sqlTypeName}' requires an explicit CLR mapping.")
        };

        if (baseType is "string" or "byte[]" or "object")
            return isNullable ? baseType + "?" : baseType;

        return isNullable ? baseType + "?" : baseType;
    }

    public static string ToSqlDbTypeExpression(string sqlTypeName)
    {
        return sqlTypeName.ToLowerInvariant() switch
        {
            "bigint" => "SqlDbType.BigInt",
            "binary" => "SqlDbType.Binary",
            "bit" => "SqlDbType.Bit",
            "char" => "SqlDbType.Char",
            "date" => "SqlDbType.Date",
            "datetime" => "SqlDbType.DateTime",
            "datetime2" => "SqlDbType.DateTime2",
            "datetimeoffset" => "SqlDbType.DateTimeOffset",
            "decimal" => "SqlDbType.Decimal",
            "float" => "SqlDbType.Float",
            "image" => "SqlDbType.Image",
            "int" => "SqlDbType.Int",
            "money" => "SqlDbType.Money",
            "nchar" => "SqlDbType.NChar",
            "ntext" => "SqlDbType.NText",
            "numeric" => "SqlDbType.Decimal",
            "nvarchar" => "SqlDbType.NVarChar",
            "real" => "SqlDbType.Real",
            "smalldatetime" => "SqlDbType.SmallDateTime",
            "smallint" => "SqlDbType.SmallInt",
            "smallmoney" => "SqlDbType.SmallMoney",
            "sql_variant" => "SqlDbType.Variant",
            "text" => "SqlDbType.Text",
            "time" => "SqlDbType.Time",
            "timestamp" or "rowversion" => "SqlDbType.Timestamp",
            "tinyint" => "SqlDbType.TinyInt",
            "uniqueidentifier" => "SqlDbType.UniqueIdentifier",
            "varbinary" => "SqlDbType.VarBinary",
            "varchar" => "SqlDbType.VarChar",
            "xml" => "SqlDbType.Xml",
            _ => throw new NotSupportedException($"SQL parameter type '{sqlTypeName}' is not supported.")
        };
    }
}
