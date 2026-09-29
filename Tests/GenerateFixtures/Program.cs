using System.Data;
using System.Text.Json;
using SqlModelGenerator.Generators;
using SqlModelGenerator.Metadata;
using SqlModelGenerator.Metadata.Models;

var target = Path.GetFullPath(args.Single());
var tsTarget = Path.GetFullPath(Path.Combine(target, "..", "..", "TypeScriptGenerated"));
var model = new DatabaseModel();
DbColumnInfo Col(string name, string sql = "int", bool nullable = false) => new()
{
    Name = name, SqlTypeName = sql, ClrTypeName = SqlTypeMapper.ToClrType(sql, nullable), IsNullable = nullable,
    MaxLength = sql == "nvarchar" ? 80 : null, Precision = sql == "decimal" ? (byte)19 : null, Scale = sql == "decimal" ? (byte)4 : null
};
DbTableInfo Table(string name, params DbColumnInfo[] columns)
{
    var table = new DbTableInfo { Schema = "dbo", Name = name };
    for (var i = 0; i < columns.Length; i++) { columns[i].Ordinal = i + 1; table.Columns.Add(columns[i]); }
    table.PrimaryKeyColumns.Add(columns[0].Name);
    model.Tables.Add(table);
    return table;
}
DbForeignKeyInfo Fk(string name, DbTableInfo dep, DbTableInfo principal, params (string, string)[] columns)
{
    var fk = new DbForeignKeyInfo { Name = name, DependentTable = dep, PrincipalTable = principal };
    foreach (var (d, p) in columns) fk.Columns.Add(new DbForeignKeyColumn(d, p));
    model.ForeignKeys.Add(fk);
    return fk;
}
void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
var customer = Table("Customer", Col("Id"), Col("Code", "nvarchar"), Col("Name", "nvarchar"), Col("ManagerId", nullable: true));
customer.Columns[0].IsIdentity = true;
var alternate = new DbUniqueKeyInfo { Name = "AK_Customer_Code" }; alternate.Columns.Add("Code"); customer.UniqueKeys.Add(alternate);
Fk("FK_Manager", customer, customer, ("ManagerId", "Id")).DeleteAction = "SET_NULL";
var order = Table("Order", Col("Id"), Col("CustomerId"), Col("ReviewerId", nullable: true), Col("Amount", "decimal"), Col("Version", "rowversion"), Col("Defaulted"), Col("Computed"));
order.Columns[5].DefaultSql = "((42))";
order.Columns[6].IsComputed = true; order.Columns[6].ComputedSql = "[Amount]*(2)"; order.Columns[6].IsStored = true;
Fk("FK_Order_Customer", order, customer, ("CustomerId", "Id")).DeleteAction = "CASCADE";
Fk("FK_Order_Reviewer", order, customer, ("ReviewerId", "Id"));
var profile = Table("Profile", Col("Id"), Col("CustomerId"));
var unique = new DbUniqueKeyInfo { Name = "UQ_Profile_Customer" }; unique.Columns.Add("CustomerId"); profile.UniqueKeys.Add(unique);
Fk("FK_Profile", profile, customer, ("CustomerId", "Id"));
var codeUse = Table("CodeUse", Col("Id"), Col("CustomerCode", "nvarchar", true));
Fk("FK_Code", codeUse, customer, ("CustomerCode", "Code"));
var parent = Table("Composite", Col("A"), Col("B")); parent.PrimaryKeyColumns.Clear(); parent.PrimaryKeyColumns.AddRange(["B", "A"]);
var child = Table("CompositeChild", Col("Id"), Col("A"), Col("B"));
Fk("FK_Composite", child, parent, ("B", "B"), ("A", "A"));
var keyless = Table("Log", Col("Id"), Col("CustomerId")); keyless.PrimaryKeyColumns.Clear();
Fk("FK_Keyless", keyless, customer, ("CustomerId", "Id"));
Fk("FK_Disabled", order, customer, ("CustomerId", "Id")).IsDisabled = true;
var legacy = Table("Legacy", Col("Id"), Col("CustomerId"));
var defaultFk = Fk("FK_Legacy", legacy, customer, ("CustomerId", "Id")); defaultFk.DeleteAction = "SET_DEFAULT"; defaultFk.UpdateAction = "CASCADE"; defaultFk.IsNotTrusted = true;
Table("Address", Col("Id")); Table("Address", Col("Id")).Schema = "archive";
Table("a-b", Col("Id")); Table("a b", Col("Id"));
Table("Quoted\"]{Table}", Col("Id"), Col("Quoted\"]{Table}"), Col("a-b", "nvarchar"), Col("a b", "nvarchar"), Col("two\"quotes", "nvarchar"));
Table("Клиент", Col("Id"), Col("Имя", "nvarchar"));
Table("SaveChange", Col("Id"), Col("GetType"), Col("ToString"), Col("Equals"));
Table("Guid", Col("Id", "uniqueidentifier")); Table("CON", Col("Id"));
var view = new DbViewInfo { Name = "CustomerView", Schema = "dbo" }; view.Columns.Add(Col("Name", "nvarchar", true)); model.Views.Add(view);
var nonquery = new DbProcedureInfo { Name = "DoWork", Schema = "dbo" };
nonquery.Parameters.Add(new() { Ordinal = 1, Name = "@class", SqlTypeName = "nvarchar", ClrTypeName = "string?", IsNullable = true, MaxLength = 80 });
nonquery.Parameters.Add(new() { Ordinal = 2, Name = "@cancellationToken", SqlTypeName = "int", ClrTypeName = "int?", IsNullable = true, IsOutput = true });
nonquery.Parameters.Add(new() { Ordinal = 3, Name = "@p0", SqlTypeName = "nvarchar", ClrTypeName = "string?", MaxLength = -1, IsOutput = true });
model.Procedures.Add(nonquery);

var rows = new DbProcedureInfo { Name = "ReadRows", Schema = "dbo" };
rows.Parameters.Add(new() { Ordinal = 1, Name = "@count", SqlTypeName = "int", ClrTypeName = "int?", IsNullable = true, IsOutput = true });
rows.ResultColumns.Add(new() { Ordinal = 1, Name = "Value", SqlTypeName = "int", ClrTypeName = "int" }); model.Procedures.Add(rows);
var unknown = new DbProcedureInfo { Name = "Unknown", Schema = "dbo", GenerationError = "Dynamic SQL shape is unknown." }; model.Procedures.Add(unknown);
var duplicate = new DbProcedureInfo { Name = "Duplicate", Schema = "dbo" };
duplicate.ResultColumns.Add(new() { Ordinal = 1, Name = "Value", SqlTypeName = "int", ClrTypeName = "int" });
duplicate.ResultColumns.Add(new() { Ordinal = 2, Name = "Value", SqlTypeName = "int", ClrTypeName = "int" });model.Procedures.Add(duplicate);
var quoted = new DbProcedureInfo { Name = "A]{B}\"", Schema = "odd]schema" }; model.Procedures.Add(quoted);

// Contracts must coexist with table names and preserve legacy method names.
Table("EchoParameters", Col("Id"));
var echo = new DbProcedureInfo { Name = "Echo", Schema = "dbo" };
echo.Parameters.Add(new() { Ordinal = 1, Name = "@filter", SqlTypeName = "nvarchar", ClrTypeName = "string?", IsNullable = true, MaxLength = 80 });
model.Procedures.Add(echo);
var onlyLong = new DbProcedureInfo { Name = "OnlyLong", Schema = "dbo" };
onlyLong.Parameters.Add(new() { Ordinal = 1, Name = "@value", SqlTypeName = "bigint", ClrTypeName = "long" });
model.Procedures.Add(onlyLong);
var defaults = new DbProcedureInfo { Name = "Defaults", Schema = "dbo" };
defaults.Parameters.Add(new() { Ordinal = 1, Name = "@first", SqlTypeName = "int", ClrTypeName = "int?", IsNullable = true });
defaults.Parameters.Add(new() { Ordinal = 2, Name = "@middle", SqlTypeName = "nvarchar", ClrTypeName = "string?", IsNullable = true, MaxLength = -1 });
defaults.Parameters.Add(new() { Ordinal = 3, Name = "@last", SqlTypeName = "int", ClrTypeName = "int" });
model.Procedures.Add(defaults);
var brokenParameters = new DbProcedureInfo { Name = "BrokenParameters", Schema = "dbo", ParametersComplete = false };
model.Procedures.Add(brokenParameters);

// Exercise DMV decoding with deliberately shuffled ordinals and exact SQL catalog data types.
var data = new DataTable();
data.Columns.Add("error_message", typeof(string)); data.Columns.Add("is_nullable", typeof(bool));
data.Columns.Add("system_type_name", typeof(string)); data.Columns.Add("column_ordinal", typeof(int));
data.Columns.Add("name", typeof(string)); data.Columns.Add("is_hidden", typeof(bool));
data.Rows.Add(DBNull.Value, true, "nvarchar(40)", 1, "Text", false);
var described = new DbProcedureInfo();
using (var reader = data.CreateDataReader()) { reader.Read(); DatabaseReader.ReadResultColumn(reader, described); }
Check(described.ResultColumns.Single().ClrTypeName == "string?", "DMV metadata decoding");
Check(ProcedureGenerator.ParameterSize(nonquery.Parameters[0]) == 40, "Unicode size is in characters");
Check(ProcedureGenerator.ParameterSize(nonquery.Parameters[2]) == -1, "MAX size retained");
Check(ProcedureGenerator.CommandText(nonquery) == "EXEC [dbo].[DoWork] @__arg0, @__arg1 OUTPUT, @__arg2 OUTPUT", "EXEC syntax / OUTPUT");
Check(ProcedureGenerator.CommandText(quoted) == "EXEC [odd]]schema].[A]]{B}\"]", "SQL identifier escaping");
Table("WireTypes", Col("Id"), Col("Active", "bit"), Col("OptionalName", "nvarchar", true), Col("CreatedAt", "datetime2"),
    Col("Duration", "time"), Col("LargeId", "bigint"), Col("Price", "decimal"), Col("Blob", "varbinary"),
    Col("Variant", "sql_variant", true), Col("Constructor"), Col("URLValue", "nvarchar"));
var mainOptions = new CodeGeneratorOptions("Smoke", Path.Combine(target, "Main"), "AppDbContext",
    BuildTypeScriptClasses: true, TypeScriptOutputFolder: Path.Combine(tsTarget, "Main"));
ModelGenerator.Generate(model, mainOptions);
Check(model.ForeignKeys.Single(x => x.Name == "FK_Profile").IsUnique, "unique FK");
Check(model.ForeignKeys.Single(x => x.Name == "FK_Keyless").SkipReason != null, "keyless FK warning");
Check(model.Warnings.Count >= 6, "warnings are visible");
Check(model.Procedures.Single(x => x.Name == "Duplicate").GenerationError != null, "duplicate SQL aliases rejected");
Check(model.Tables.Select(t => t.ClassName).Distinct(StringComparer.OrdinalIgnoreCase).Count() == model.Tables.Count, "type names distinct");
var originalNames = model.Tables.Select(t => t.ClassName).ToArray();
ModelPreparation.Prepare(model, "AppDbContext");
Check(originalNames.SequenceEqual(model.Tables.Select(t => t.ClassName)), "names deterministic across regeneration");
var expectations = model.ForeignKeys.Where(f => f.SkipReason == null).Select(f => new {
    f.Name, Dependent = f.DependentTable.ClassName, Principal = f.PrincipalTable.ClassName,
    DependentColumns = f.Columns.Select(c => f.DependentTable.Columns.Single(p => p.Name == c.DependentColumn).PropertyName),
    PrincipalColumns = f.Columns.Select(c => f.PrincipalTable.Columns.Single(p => p.Name == c.PrincipalColumn).PropertyName),
    f.IsUnique, f.IsRequired, f.DependentNavigation, f.PrincipalNavigation, f.DeleteAction
});
File.WriteAllText(Path.Combine(target, "expected.json"), JsonSerializer.Serialize(expectations));
ModelGenerator.Generate(new(), new("EmptySmoke", Path.Combine(target, "Empty"), "EmptyContext"));
var onlyView = new DatabaseModel(); onlyView.Views.Add(view);
ModelGenerator.Generate(onlyView, new("ViewSmoke", Path.Combine(target, "ViewOnly"), "ViewContext"));
var onlyProc = new DatabaseModel(); onlyProc.Procedures.Add(rows);
ModelGenerator.Generate(onlyProc, new("ProcSmoke", Path.Combine(target, "ProcOnly"), "ProcContext"));

// Generated output owns only its manifest files, never unrelated partial classes.
var lifecycle = Path.Combine(target, "Lifecycle");
var tempModel = new DatabaseModel(); var tempTable = new DbTableInfo { Schema = "dbo", Name = "Old" };
tempTable.Columns.Add(Col("Id"));tempTable.PrimaryKeyColumns.Add("Id");tempModel.Tables.Add(tempTable);
var lifecycleOptions = new CodeGeneratorOptions("LifecycleSmoke", lifecycle, "LifecycleContext");
ModelGenerator.Generate(tempModel, lifecycleOptions);
File.WriteAllText(Path.Combine(lifecycle, "Keep.txt"), "user file");
tempTable.Name = "New"; ModelGenerator.Generate(tempModel, lifecycleOptions);
Check(!File.Exists(Path.Combine(lifecycle, "Entities", "Old.cs")), "obsolete generated file removed");
Check(File.ReadAllText(Path.Combine(lifecycle, "Keep.txt")) == "user file", "user file retained");
Console.WriteLine($"PASS: metadata, names, procedure SQL/size, output lifecycle; generated {model.Tables.Count} tables, {model.ForeignKeys.Count} foreign keys and 5 model variants.");

Check(TypeScriptGenerator.JsonPropertyName("URLValue") == "urlValue", "ASP.NET acronym naming");
Check(TypeScriptTypeMapper.ToType("bigint", false) == "number", "bigint follows JSON wire type");
Check(TypeScriptTypeMapper.ToType("nvarchar", true) == "string | null", "nullable TypeScript scalar");
Check(TypeScriptTypeMapper.ToType("datetime2", false) == "string", "dates use JSON string");
Check(TypeScriptTypeMapper.ToType("varbinary", false) == "string", "byte arrays use base64 string");
Check(TypeScriptTypeMapper.Note("decimal") != null, "decimal precision note");
Check(!File.Exists(Path.Combine(tsTarget, "Main", "procedures", "UnknownResult.ts")), "unsupported procedure omitted in TypeScript");
Check(File.Exists(Path.Combine(tsTarget, "Main", "procedures", "ReadRowsResult.ts")), "procedure result generated in TypeScript");
var disabledOptions = new CodeGeneratorOptions("DisabledSmoke", Path.Combine(tsTarget, "DisabledCSharp"), "DisabledContext");
Check(!disabledOptions.ShouldBuildTypeScript, "old three-argument callers default off");
Check(!(disabledOptions with { BuildTypeScriptClasses = true }).ShouldBuildTypeScript, "missing folder means off");
Check(!(disabledOptions with { BuildTypeScriptClasses = true, TypeScriptOutputFolder = "  " }).ShouldBuildTypeScript, "blank folder means off");
var unusedFolder = Path.Combine(tsTarget, "NotCreated");
ModelGenerator.Generate(new(), disabledOptions with { TypeScriptOutputFolder = unusedFolder });
Check(!Directory.Exists(unusedFolder), "folder alone cannot enable TypeScript");
ModelGenerator.Generate(new(), disabledOptions with { BuildTypeScriptClasses = true });
var existingTs = File.ReadAllText(Path.Combine(tsTarget, "Main", "index.ts"));
ModelGenerator.Generate(new(), disabledOptions with { TypeScriptOutputFolder = Path.Combine(tsTarget, "Main") });
Check(existingTs == File.ReadAllText(Path.Combine(tsTarget, "Main", "index.ts")), "disabled generation preserves existing TypeScript");
try
{
    ModelGenerator.Generate(new(), disabledOptions with { BuildTypeScriptClasses = true, TypeScriptOutputFolder = disabledOptions.OutputRoot });
    throw new Exception("same output directory accepted");
}
catch (ArgumentException) { }
var tsLifecycleOptions = lifecycleOptions with { OutputRoot = Path.Combine(tsTarget, "LifecycleCSharp"),
    BuildTypeScriptClasses = true, TypeScriptOutputFolder = Path.Combine(tsTarget, "Lifecycle") };
tempTable.Name = "Old"; ModelGenerator.Generate(tempModel, tsLifecycleOptions);
File.WriteAllText(Path.Combine(tsLifecycleOptions.TypeScriptOutputFolder!, "Keep.ts"), "// user file");
tempTable.Name = "New"; ModelGenerator.Generate(tempModel, tsLifecycleOptions);
Check(!File.Exists(Path.Combine(tsLifecycleOptions.TypeScriptOutputFolder!, "entities", "Old.ts")), "obsolete TypeScript removed");
Check(File.ReadAllText(Path.Combine(tsLifecycleOptions.TypeScriptOutputFolder!, "Keep.ts")) == "// user file", "custom TypeScript retained");
var clashFolder = Path.Combine(tsTarget, "Clash"); Directory.CreateDirectory(clashFolder);
File.WriteAllText(Path.Combine(clashFolder, "index.ts"), "// user-owned index");
var csBefore = File.ReadAllText(Path.Combine(disabledOptions.OutputRoot, "DisabledContext.g.cs"));
try
{
    ModelGenerator.Generate(tempModel, disabledOptions with { BuildTypeScriptClasses = true, TypeScriptOutputFolder = clashFolder });
    throw new Exception("user TypeScript overwritten");
}
catch (IOException) { }
Check(File.ReadAllText(Path.Combine(clashFolder, "index.ts")) == "// user-owned index", "TypeScript collision preserves user file");
Check(File.ReadAllText(Path.Combine(disabledOptions.OutputRoot, "DisabledContext.g.cs")) == csBefore, "TypeScript collision prevents C# publication too");
ModelGenerator.Generate(new(), disabledOptions with { BuildTypeScriptClasses = true, TypeScriptOutputFolder = Path.Combine(tsTarget, "Empty") });
Check(File.ReadAllText(Path.Combine(tsTarget, "Empty", "index.ts")).Contains("export {};"), "empty TypeScript remains a module");
Console.WriteLine("PASS: optional TypeScript flags/folder, JSON names/types, nullability, procedure results, protected publication and regeneration.");

Check(echo.MethodName == "Echo" && echo.ParametersClassName == "EchoParameters2", "new DTO name does not rename legacy method or table");
Check(!File.Exists(Path.Combine(mainOptions.OutputRoot, "Procedures", brokenParameters.ParametersClassName + ".cs")), "incomplete parameter list does not generate misleading C# DTO");
Check(!File.Exists(Path.Combine(tsTarget, "Main", "procedures", brokenParameters.ParametersClassName + ".ts")), "incomplete parameter list does not generate misleading TS DTO");
Check(File.Exists(Path.Combine(tsTarget, "Main", "procedures", unknown.ParametersClassName + ".ts")), "known parameters survive unknown result schema");
Check(File.Exists(Path.Combine(mainOptions.OutputRoot, "Procedures", nonquery.OutputClassName + ".cs")), "typed OUTPUT class generated");
Console.WriteLine("PASS: procedure parameter/output contracts, name collisions and incomplete metadata handling.");
