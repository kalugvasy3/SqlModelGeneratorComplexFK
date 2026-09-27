using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
// Constructing a model and translating SQL does not open a database connection.
var options = new DbContextOptionsBuilder<Smoke.AppDbContext>().UseSqlServer("Server=unused;Database=unused;Integrated Security=true;Encrypt=false").Options;
using var context = new Smoke.AppDbContext(options);
var model = context.Model;
using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Generated", "expected.json")));
int count = 0;
foreach (var item in expected.RootElement.EnumerateArray())
{
    string Text(string key) => item.GetProperty(key).GetString()!;
    string[] Strings(string key) => item.GetProperty(key).EnumerateArray().Select(e => e.GetString()!).ToArray();
    var entity = model.GetEntityTypes().Single(e => e.ClrType.Name == Text("Dependent"));
    var fk = entity.GetForeignKeys().Single(f => f.GetConstraintName() == Text("Name"));
    Check(fk.PrincipalEntityType.ClrType.Name == Text("Principal"), "principal");
    Check(fk.Properties.Select(p => p.Name).SequenceEqual(Strings("DependentColumns")), "FK column order");
    Check(fk.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual(Strings("PrincipalColumns")), "principal key order");
    Check(fk.IsUnique == item.GetProperty("IsUnique").GetBoolean(), "cardinality");
    Check(fk.IsRequired == item.GetProperty("IsRequired").GetBoolean(), "requiredness");
    Check(fk.DependentToPrincipal?.Name == Text("DependentNavigation"), "dependent navigation");
    Check(fk.PrincipalToDependent?.Name == Text("PrincipalNavigation"), "principal navigation");
    Check(fk.DeleteBehavior == (Text("DeleteAction") switch { "CASCADE" => DeleteBehavior.Cascade, "SET_NULL" => DeleteBehavior.SetNull, _ => DeleteBehavior.NoAction }), "delete behavior");
    count++;
}
var composite = model.FindEntityType(typeof(Smoke.Entities.Composite))!;
Check(composite.FindPrimaryKey()!.Properties.Select(p => p.Name).SequenceEqual(new[] { "B", "A" }), "PK order");
var order = model.FindEntityType(typeof(Smoke.Entities.Order))!;
Check(order.FindProperty("Amount")!.GetColumnType() == "decimal(19,4)", "decimal precision");
Check(order.FindProperty("Id")!.ValueGenerated == ValueGenerated.Never, "nonidentity PK");
Check(order.FindProperty("Version")!.IsConcurrencyToken, "rowversion");
Check(order.FindProperty("Defaulted")!.GetDefaultValueSql() == "((42))", "default SQL");
Check(order.FindProperty("Computed")!.GetComputedColumnSql() == "[Amount]*(2)", "computed SQL");
Check(model.FindEntityType(typeof(Smoke.Entities.Customer))!.FindProperty("Name")!.GetColumnType() == "nvarchar(40)", "Unicode store length");
Check(model.FindEntityType(typeof(Smoke.Entities.Customer))!.FindProperty("Id")!.ValueGenerated == ValueGenerated.OnAdd, "identity");
Check(model.FindEntityType(typeof(Smoke.Entities.Log))!.GetForeignKeys().Count() == 0, "keyless relationships skipped");
Check(model.GetEntityTypes().Sum(e => e.GetForeignKeys().Count()) == count, "no shadow/unintended relationships");
using var empty = new EmptySmoke.EmptyContext(new DbContextOptionsBuilder<EmptySmoke.EmptyContext>().UseSqlServer("Server=unused").Options);
Check(!empty.Model.GetEntityTypes().Any(), "empty schema");
using var views = new ViewSmoke.ViewContext(new DbContextOptionsBuilder<ViewSmoke.ViewContext>().UseSqlServer("Server=unused").Options);
Check(views.Model.GetEntityTypes().Single().FindPrimaryKey() == null, "view-only schema");
using var procs = new ProcSmoke.ProcContext(new DbContextOptionsBuilder<ProcSmoke.ProcContext>().UseSqlServer("Server=unused").Options);
Check(procs.Model.GetEntityTypes().Single().FindPrimaryKey() == null, "procedure-only schema");
try { await context.UnknownAsync(); throw new Exception("Unknown procedure executed"); }
catch (NotSupportedException) { }
try { await context.DuplicateAsync(); throw new Exception("Ambiguous procedure executed"); }
catch (NotSupportedException) { }
Check(context.Set<Smoke.Procedures.ReadRowsResult>().FromSqlRaw("EXEC [dbo].[ReadRows]").ToQueryString().Contains("EXEC [dbo].[ReadRows]"), "procedure SQL translation");
var commands = new CapturedCommands();
var commandOptions = new DbContextOptionsBuilder<Smoke.AppDbContext>().UseSqlServer("Server=unused;Database=unused;Integrated Security=true;Encrypt=false")
    .AddInterceptors(new NeverConnect(), commands).Options;
using var executionContext = new Smoke.AppDbContext(commandOptions);
using var cancellation = new CancellationTokenSource();
var output = new Dictionary<string, object?>();
var affected = await executionContext.DoWorkAsync("text", null, null, cancellation.Token, output);
Check(affected == 3, "nonquery result");
Check(commands.Text == "EXEC [dbo].[DoWork] @__arg0, @__arg1 OUTPUT, @__arg2 OUTPUT", "actual SQL");
Check(commands.Token == cancellation.Token, "cancellation overload");
Check(commands.Sizes.SequenceEqual(new[] { 40, 0, -1 }), "actual parameter lengths");
Check((int)output["@cancellationToken"]! == 42 && output["@p0"] == null, "output values including DBNull");
output.Clear();
var records = await executionContext.ReadRowsAsync(null, cancellation.Token, output);
Check(records.Single().Value == 123, "result materialized");
Check((int)output["@count"]! == 42, "result procedure output exposed");
Check(commands.Token == cancellation.Token, "query cancellation propagated");
await executionContext.ABAsync(cancellation.Token);
Check(commands.Text == "EXEC [odd]]schema].[A]]{B}\"]", "brackets, braces and quotes survive EF formatting");
Console.WriteLine("PASS: procedure wrappers intercepted locally: SQL, parameter sizes, OUTPUT/DBNull, cancellation and row materialization. SQL connections suppressed.");
Console.WriteLine($"PASS: compiled generated code; EF validates {count} foreign keys, composite/alternate keys, mapping facets and empty/view/procedure-only models. No database connection opened.");
