using Mark.Analyzers;
using Microsoft.Extensions.DependencyInjection;

namespace Mark.Factory.SimpleTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Testing Mark.Analyzers Source Generator...");
        Console.WriteLine("Build completed successfully!");
        Console.WriteLine("Check the Generated folder for the source generator output.");

        var services = new ServiceCollection();

        services.AddMarkFactorySimpleTestServices();

        var serviceProvider = services.BuildServiceProvider();

        var simpleService = serviceProvider.GetRequiredService<ISimpleService>();

        simpleService.GetMessage();
    }
}