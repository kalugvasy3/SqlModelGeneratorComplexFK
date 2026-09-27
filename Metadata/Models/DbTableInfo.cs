namespace SqlModelGenerator.Metadata.Models;

public sealed class DbTableInfo : DbObjectBase
{
    public List<DbUniqueKeyInfo> UniqueKeys { get; } = new();
    public List<DbForeignKeyInfo> OutgoingForeignKeys { get; } = new();
    public List<DbForeignKeyInfo> IncomingForeignKeys { get; } = new();
    public List<string> PrimaryKeyColumns { get; } = new();
}
