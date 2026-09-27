using SqlModelGenerator.Metadata.Models;

namespace SqlModelGenerator.Metadata;

public static class ModelPreparation
{
    private static HashSet<string> MemberNames(string owner) => new(StringComparer.OrdinalIgnoreCase)
    {
        owner.TrimStart('@'), "Equals", "GetHashCode", "GetType", "ToString", "ReferenceEquals", "MemberwiseClone", "Finalize"
    };
    public static void Prepare(DatabaseModel model, string contextName)
    {
        var usedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            contextName, "Entities", "Views", "Procedures", "DbContext", "DbSet", "DbContextOptions",
            "ModelBuilder", "DeleteBehavior", "Task", "List", "SqlParameter", "SqlDbType", "ParameterDirection",
            "CancellationToken", "DBNull", "IDictionary", "NotSupportedException", "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        var objects = model.Tables.Cast<DbObjectBase>().Concat(model.Views).OrderBy(x => x.Schema, StringComparer.Ordinal)
            .ThenBy(x => x.Name, StringComparer.Ordinal).ToList();
        var duplicateNames = objects.GroupBy(x => NameHelper.ToPascalCase(x.Name), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var members = new Dictionary<DbTableInfo, HashSet<string>>();
        foreach (var item in objects)
        {
            var desired = NameHelper.ToPascalCase(item.Name);
            if (duplicateNames.Contains(desired)) desired = NameHelper.ToPascalCase(item.Schema) + desired;
            item.ClassName = NameHelper.Unique(desired, usedTypes);
            var used = MemberNames(item.ClassName);
            foreach (var c in item.Columns.OrderBy(c => c.Ordinal))
                c.PropertyName = NameHelper.Unique(NameHelper.ToPascalCase(c.Name), used);
            if (item is DbTableInfo table)
            {
                members[table] = used;
                table.OutgoingForeignKeys.Clear();
                table.IncomingForeignKeys.Clear();
            }
        }
        var contextMembers = MemberNames(contextName);
        contextMembers.UnionWith(new[] { "Database", "ChangeTracker", "Model", "ContextId", "SaveChanges", "SaveChangesAsync",
            "Dispose", "DisposeAsync", "Add", "AddAsync", "AddRange", "AddRangeAsync", "Find", "FindAsync", "Set", "Entry",
            "Attach", "AttachRange", "Update", "UpdateRange", "Remove", "RemoveRange", "OnModelCreating", "OnConfiguring", "ConfigureConventions" });
        foreach (var item in objects) item.SetName = NameHelper.Unique(item.ClassName + "s", contextMembers);
        var duplicateProcedures = model.Procedures.GroupBy(x => NameHelper.ToPascalCase(x.Name), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var proc in model.Procedures.OrderBy(x => x.Schema, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            var desired = NameHelper.ToPascalCase(proc.Name);
            if (duplicateProcedures.Contains(desired))
                desired = NameHelper.ToPascalCase(proc.Schema) + desired;
            var name = desired;
            for (var suffix = 2; usedTypes.Contains(name + "Result") || contextMembers.Contains(name + "Async"); suffix++) name = desired + suffix;
            proc.MethodName = name;
            usedTypes.Add(proc.ResultClassName);
            contextMembers.Add(name + "Async");
            proc.SetName = NameHelper.Unique(proc.ResultClassName + "s", contextMembers);
            // Keep helper/local names separate from user parameter names.
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cancellationToken", "outputValues", "sqlParameters", "result" };
            for (var i = 0; i < proc.Parameters.Count; i++) used.Add("p" + i);
            foreach (var param in proc.Parameters.OrderBy(x => x.Ordinal))
                param.VariableName = NameHelper.Unique(NameHelper.ToCamelCase(param.Name.TrimStart('@')), used);
            used = MemberNames(proc.ResultClassName);
            foreach (var c in proc.ResultColumns.OrderBy(x => x.Ordinal))
                c.PropertyName = NameHelper.Unique(NameHelper.ToPascalCase(c.Name), used);
            if (proc.ResultColumns.Any(c => string.IsNullOrEmpty(c.Name)) || proc.ResultColumns.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                proc.GenerationError = "Result columns must have distinct, nonempty SQL aliases for EF materialization.";
            if (proc.GenerationError != null) Warn(model, $"Procedure [{proc.Schema}].[{proc.Name}]: {proc.GenerationError}");
        }
        foreach (var fk in model.ForeignKeys.OrderBy(x => x.DependentTable.Schema, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            fk.SkipReason = null;
            var dependent = fk.DependentTable;
            var principal = fk.PrincipalTable;
            if (fk.IsDisabled) fk.SkipReason = "Constraint is disabled.";
            else if (dependent.PrimaryKeyColumns.Count == 0 || principal.PrimaryKeyColumns.Count == 0)
                fk.SkipReason = "A table has no primary key; bidirectional EF navigation requires keyed entities.";
            else if (fk.Columns.Count == 0 || fk.Columns.Any(c => !dependent.Columns.Any(d => d.Name == c.DependentColumn) || !principal.Columns.Any(p => p.Name == c.PrincipalColumn)))
                fk.SkipReason = "Incomplete column metadata.";
            else if (fk.Columns.Any(c => principal.Columns.Single(p => p.Name == c.PrincipalColumn).IsNullable))
                fk.SkipReason = "Nullable principal key cannot be represented as an EF alternate key without changing nullability.";
            if (fk.SkipReason != null)
            {
                Warn(model, $"FK [{dependent.Schema}].[{fk.Name}] skipped: {fk.SkipReason}");
                continue;
            }
            var dependentColumns = fk.Columns.Select(c => c.DependentColumn).ToHashSet(StringComparer.Ordinal);
            fk.IsUnique = dependent.PrimaryKeyColumns.All(dependentColumns.Contains)
                || dependent.UniqueKeys.Any(k => !k.IsFiltered && k.Columns.Count > 0 && k.Columns.All(dependentColumns.Contains));
            fk.IsRequired = fk.Columns.All(c => !dependent.Columns.Single(d => d.Name == c.DependentColumn).IsNullable);
            var nav = principal.ClassName;
            if (fk.Columns.Count == 1)
            {
                var col = NameHelper.ToPascalCase(fk.Columns[0].DependentColumn);
                if (col.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && col.Length > 2) nav = col[..^2];
            }
            fk.DependentNavigation = NameHelper.Unique(nav, members[dependent]);
            fk.PrincipalNavigation = NameHelper.Unique(dependent.ClassName + (fk.IsUnique ? "" : "s") + "Via" + fk.DependentNavigation.TrimStart('@'), members[principal]);
            dependent.OutgoingForeignKeys.Add(fk);
            principal.IncomingForeignKeys.Add(fk);
            if (fk.IsNotTrusted) Warn(model, $"FK [{dependent.Schema}].[{fk.Name}] is not trusted; existing data may violate it.");
            if (fk.DeleteAction == "SET_DEFAULT") Warn(model, $"FK [{dependent.Schema}].[{fk.Name}]: SQL SET DEFAULT is retained in the database but represented as EF NoAction; do not recreate this constraint through migrations.");
            if (fk.UpdateAction != "NO_ACTION") Warn(model, $"FK [{dependent.Schema}].[{fk.Name}]: ON UPDATE {fk.UpdateAction} is database-only; EF does not model update actions.");
        }
    }

    private static void Warn(DatabaseModel model, string text)
    {
        if (!model.Warnings.Contains(text)) model.Warnings.Add(text);
    }
}
