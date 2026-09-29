using SqlModelGenerator.Generators;
using SqlModelGenerator.Metadata;

namespace SqlModelGenerator;

internal static class Program
{
    // Optional TypeScript output. Both a true flag and a nonempty folder are required.
    // These defaults can also be overridden with the environment variables shown below.
    private const bool BuildTypeScriptClasses = false;
    private const string? TypeScriptOutputFolder = null; // Example: @"C:\GeneratedModels\TypeScript"

    // TODO: put your real connection string here.
    private const string ConnectionString =
                       // "Data Source=(local);Initial Catalog=MeasureReader;Integrated Security=True;MultipleActiveResultSets=True;Encrypt=false;TrustServerCertificate=true";
                       @"
            Data Source=localhost;
            Initial Catalog = OregonLC; 
            MultipleActiveResultSets=True;
            Encrypt=false;
            TrustServerCertificate=true;
            Integrated Security = True;
            Transaction Binding = Explicit Unbind;";
    // MeasuresLC;




    private static bool ReadBooleanSetting(string name, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (bool.TryParse(value, out var enabled)) return enabled;
        throw new ArgumentException($"{name} must be True or False.");
    }

    private static async Task<int> Main()
    {
        try
        {
            var outputRoot = Environment.GetEnvironmentVariable("SQLMODELGENERATOR_OUTPUT")
                ?? Path.Combine(AppContext.BaseDirectory, "Generated");
            var buildTypeScript = ReadBooleanSetting("SQLMODELGENERATOR_BUILD_TYPESCRIPT_CLASSES", BuildTypeScriptClasses);
            var typeScriptFolder = Environment.GetEnvironmentVariable("SQLMODELGENERATOR_TYPESCRIPT_OUTPUT") ?? TypeScriptOutputFolder;
            var options = new CodeGeneratorOptions(
                RootNamespace: "GeneratedModel",
                OutputRoot: outputRoot,
                DbContextName: "AppDbContext",
                BuildTypeScriptClasses: buildTypeScript,
                TypeScriptOutputFolder: typeScriptFolder);
            ModelGenerator.ValidateOptions(options);

            Console.WriteLine("Reading SQL Server metadata...");
            var reader = new DatabaseReader(Environment.GetEnvironmentVariable("SQLMODELGENERATOR_CONNECTION_STRING") ?? ConnectionString);
            var model = await reader.ReadAsync();

            Console.WriteLine($"Tables     : {model.Tables.Count}");
            Console.WriteLine($"Views      : {model.Views.Count}");
            Console.WriteLine($"Procedures : {model.Procedures.Count}");

            ModelGenerator.Generate(model, options);

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.WriteLine($"Output folder: {outputRoot}");
            Console.WriteLine(options.ShouldBuildTypeScript
                ? $"TypeScript output folder: {options.TypeScriptOutputFolder}"
                : "TypeScript generation: disabled (requires True and an output folder).");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
