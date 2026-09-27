namespace SqlModelGenerator.Metadata.Models;

public sealed class DatabaseModel
{
    public List<DbForeignKeyInfo> ForeignKeys { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<DbTableInfo> Tables { get; } = new();
    public List<DbViewInfo> Views { get; } = new();
    public List<DbProcedureInfo> Procedures { get; } = new();
}
