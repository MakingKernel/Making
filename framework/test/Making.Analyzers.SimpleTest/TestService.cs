namespace Making.Factory.SimpleTest;

public interface ISimpleService
{
    string GetMessage();
}

[Scoped]
public class SimpleService : ISimpleService
{
    public string GetMessage() => "Hello from SimpleService!";
}

[Singleton]
[RegisterService(typeof(ISimpleService))]
public class AnotherSimpleService : ISimpleService
{
    public string GetMessage() => "Hello from AnotherSimpleService!";
}

[Transient]
public class LogService
{
    public void Log(string message) => Console.WriteLine($"LOG: {message}");
}
