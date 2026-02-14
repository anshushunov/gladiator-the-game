namespace Ludus.Core;

/// <summary>
/// Исключение для ошибок валидации доменных объектов.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>
/// Исключение для ошибок, связанных с состоянием симуляции.
/// </summary>
public class LudusException : Exception
{
    public LudusException(string message) : base(message) { }
}

/// <summary>
/// Исключение для ошибок генерации имён (например, исчерпание пула).
/// </summary>
public class NameGenerationException : Exception
{
    public NameGenerationException(string message) : base(message) { }
}
