using Microsoft.Extensions.DependencyInjection;

namespace Making.Analyzers.SimpleTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Testing Making.Analyzers Source Generator...");
        Console.WriteLine("Build completed successfully!");
        Console.WriteLine("Check the Generated folder for the source generator output.");

        var services = new ServiceCollection();

        services.AddMakingAnalyzersSimpleTestServices();

        var serviceProvider = services.BuildServiceProvider();

        var simpleService = serviceProvider.GetRequiredService<ISimpleService>();

        simpleService.GetMessage();
    }
}