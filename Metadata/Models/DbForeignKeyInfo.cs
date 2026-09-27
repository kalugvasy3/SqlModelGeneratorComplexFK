namespace SqlModelGenerator.Metadata.Models;

public sealed class DbForeignKeyInfo
{
    public string Name { get; set; } = "";
    public DbTableInfo DependentTable { get; set; } = null!;
    public DbTableInfo PrincipalTable { get; set; } = null!;
    // Position in this list is constraint_column_id, not table column order.
    public List<DbForeignKeyColumn> Columns { get; } = new();
    public string DeleteAction { get; set; } = "NO_ACTION";
    public string UpdateAction { get; set; } = "NO_ACTION";
    public bool IsDisabled { get; set; }
    public bool IsNotTrusted { get; set; }
    public bool IsUnique { get; set; }
    public bool IsRequired { get; set; }
    public string DependentNavigation { get; set; } = "";
    public string PrincipalNavigation { get; set; } = "";
    public string? SkipReason { get; set; }
}

public sealed record DbForeignKeyColumn(string DependentColumn, string PrincipalColumn);

public sealed class DbUniqueKeyInfo
{
    public string Name { get; set; } = "";
    public List<string> Columns { get; } = new();
    public bool IsFiltered { get; set; }
}
