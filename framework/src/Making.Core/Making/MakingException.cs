namespace Making;

/// <summary>
/// 这是由Making系统针对性特定的MakingException的基类
/// </summary>
public class MakingException : Exception
{
    public MakingException()
    {

    }

    public MakingException(string? message)
        : base(message)
    {

    }

    public MakingException(string? message, Exception? innerException)
        : base(message, innerException)
    {

    }
}