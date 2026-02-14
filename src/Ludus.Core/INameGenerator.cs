using System.Collections.Generic;

namespace Ludus.Core;

/// <summary>
/// Интерфейс генератора уникальных имён на основе prefixes и cognomens.
/// Обеспечивает детерминизм через seed и выбор без повторов.
/// </summary>
public interface INameGenerator
{
    /// <summary>
    /// Выдаёт следующее имя из пула.
    /// Выбрасывает исключение, если пул исчерпан.
    /// </summary>
    /// <returns>Сгенерированное имя.</returns>
    /// <exception cref="NameGenerationException">Когда пул имён исчерпан.</exception>
    string GenerateNext();

    /// <summary>
    /// Пытается выдать следующее имя из пула.
    /// Возвращает false, если пул исчерпан.
    /// </summary>
    /// <param name="name">Выходной параметр для сгенерированного имени.</param>
    /// <returns>True, если имя успешно сгенерировано; false, если пул исчерпан.</returns>
    bool TryGenerate(out string name);
}
