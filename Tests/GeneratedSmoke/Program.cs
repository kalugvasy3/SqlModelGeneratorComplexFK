using System;
using System.IO;
using System.Text.Json.Serialization;
using Smoke.Procedures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
// Legacy null/default and implicit numeric conversions must still compile and bind to the old methods.
await executionContext.EchoAsync(null);
Check(commands.InputValues.Single() is DBNull, "legacy null argument remains SQL NULL");
await executionContext.EchoAsync(default);
await executionContext.OnlyLongAsync(1);
Check(commands.InputValues.Single() is long, "legacy numeric conversion remains bound to scalar overload");

var typedOutput = new DoWorkOutput();
var typedParameters = new DoWorkParameters { Class = "dto", CancellationToken = null, P0 = null };
affected = await executionContext.DoWorkAsync(typedParameters, cancellation.Token, typedOutput);
Check(affected == 3 && typedOutput.CancellationToken == 42 && typedOutput.P0 == null, "typed output values / DBNull");
Check(commands.Token == cancellation.Token && commands.Sizes.SequenceEqual(new[] { 40, 0, -1 }), "DTO cancellation / parameter facets");
Check((string)commands.InputValues[0]! == "dto" && commands.InputValues[1] is DBNull, "DTO initial values");
var readOutput = new ReadRowsOutput();
records = await executionContext.ReadRowsAsync(new ReadRowsParameters { Count = 5 }, cancellation.Token, readOutput);
Check(records.Single().Value == 123 && readOutput.Count == 42 && (int)commands.InputValues.Single()! == 5, "result plus INPUT/OUTPUT initial value");
var countBefore = commands.CommandCount;
try { await executionContext.ReadRowsAsync(new ReadRowsParameters()); throw new Exception("Missing OUTPUT accepted"); }
catch (ArgumentException) { }
Check(commands.CommandCount == countBefore, "unassigned OUTPUT rejected before command execution");
try { await executionContext.EchoAsync((EchoParameters2)null!); throw new Exception("Null DTO accepted"); }
catch (ArgumentNullException) { }
Check(commands.CommandCount == countBefore, "null DTO rejected before command execution");
await executionContext.EchoAsync(new EchoParameters2());
Check(commands.Text == "EXEC [dbo].[Echo] DEFAULT" && commands.InputValues.Length == 0, "omitted input requests DEFAULT");
await executionContext.EchoAsync(new EchoParameters2 { Filter = null });
Check(commands.Text == "EXEC [dbo].[Echo] @__arg0" && commands.InputValues.Single() is DBNull, "explicit DTO null is SQL NULL");
await executionContext.DefaultsAsync(new DefaultsParameters { First = null, Last = 0 });
Check(commands.Text == "EXEC [dbo].[Defaults] @__arg0, DEFAULT, @__arg2", "middle omission preserves parameter order");
Check(commands.InputValues.Length == 2 && commands.InputValues[0] is DBNull && (int)commands.InputValues[1]! == 0, "explicit null and zero are supplied");
await executionContext.ABAsync(new ABParameters());
Check(commands.Text == "EXEC [odd]]schema].[A]]{B}\"]", "empty DTO and escaped procedure name");
try { await executionContext.UnknownAsync(new UnknownParameters()); throw new Exception("Unknown DTO procedure executed"); }
catch (NotSupportedException) { }

try { await executionContext.BrokenParametersAsync(); throw new Exception("Incomplete procedure executed"); }
catch (NotSupportedException) { }

// ASP.NET Core's default web JSON settings preserve flat DTO values, nulls and omissions.
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
var absentDto = JsonSerializer.Deserialize<EchoParameters2>("{}", jsonOptions)!;
var nullDto = JsonSerializer.Deserialize<EchoParameters2>("{\"filter\":null}", jsonOptions)!;
Check(!absentDto.Filter.IsSpecified && nullDto.Filter.IsSpecified && nullDto.Filter.GetValue() == null, "JSON omission distinct from null");
Check(JsonSerializer.Serialize(absentDto, jsonOptions) == "{}", "unset input omitted when serializing");
Check(JsonSerializer.Serialize(nullDto, jsonOptions) == "{\"filter\":null}", "explicit null preserved even with global ignore-null");
var numericDto = JsonSerializer.Deserialize<DefaultsParameters>("{\"first\":null,\"last\":0}", jsonOptions)!;
Check(numericDto.First.IsSpecified && numericDto.Last.IsSpecified && !numericDto.Middle.IsSpecified && numericDto.Last.GetValue() == 0, "numeric presence survives JSON");
Check(JsonSerializer.Serialize(numericDto, jsonOptions) == "{\"first\":null,\"last\":0}", "DTO serializes to plain camelCase object");
var jsonDto = JsonSerializer.Deserialize<DoWorkParameters>("{\"class\":\"json\",\"cancellationToken\":null,\"p0\":null}", jsonOptions)!;
await executionContext.DoWorkAsync(jsonDto, cancellation.Token, typedOutput);
Check(commands.InputValues[0] is "json" && typedOutput.CancellationToken == 42, "API DTO can drive procedure overload");
Check(JsonSerializer.Serialize(typedOutput, new JsonSerializerOptions(JsonSerializerDefaults.Web)) == "{\"cancellationToken\":42,\"p0\":null}", "typed output serializes directly");
var services = new ServiceCollection();
services.AddLogging();
services.AddControllers();
using var provider = services.BuildServiceProvider();
var action = new ActionContext(new DefaultHttpContext { RequestServices = provider }, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
provider.GetRequiredService<IObjectModelValidator>().Validate(action, null, "", absentDto);
Check(action.ModelState.IsValid, "MVC accepts an omitted DEFAULT argument without traversing a throwing Value getter");
provider.GetRequiredService<IObjectModelValidator>().Validate(action, null, "", jsonDto);
Check(action.ModelState.IsValid, "MVC accepts explicitly null INPUT/OUTPUT properties");
Console.WriteLine("PASS: DTO overloads, typed outputs, old-call compatibility, SQL DEFAULT versus NULL, JSON presence/round-trip and cancellation.");
Console.WriteLine("PASS: procedure wrappers intercepted locally: SQL, parameter sizes, OUTPUT/DBNull, cancellation and row materialization. SQL connections suppressed.");
Console.WriteLine($"PASS: compiled generated code; EF validates {count} foreign keys, composite/alternate keys, mapping facets and empty/view/procedure-only models. No database connection opened.");
