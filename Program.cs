using SqlModelGenerator.Generators;
using SqlModelGenerator.Metadata;

namespace SqlModelGenerator;

internal static class Program
{
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




    private static async Task<int> Main()
    {
        try
        {
            var outputRoot = Environment.GetEnvironmentVariable("SQLMODELGENERATOR_OUTPUT")
                ?? Path.Combine(AppContext.BaseDirectory, "Generated");
            Directory.CreateDirectory(outputRoot);

            Console.WriteLine("Reading SQL Server metadata...");
            var reader = new DatabaseReader(Environment.GetEnvironmentVariable("SQLMODELGENERATOR_CONNECTION_STRING") ?? ConnectionString);
            var model = await reader.ReadAsync();

            Console.WriteLine($"Tables     : {model.Tables.Count}");
            Console.WriteLine($"Views      : {model.Views.Count}");
            Console.WriteLine($"Procedures : {model.Procedures.Count}");

            var options = new CodeGeneratorOptions(
                RootNamespace: "GeneratedModel",
                OutputRoot: outputRoot,
                DbContextName: "AppDbContext");

            ModelGenerator.Generate(model, options);

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.WriteLine($"Output folder: {outputRoot}");
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
