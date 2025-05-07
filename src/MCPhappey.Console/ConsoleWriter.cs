
using System.Text;
using System.Text.Json;

public static class ConsoleWriter
{
    public static void WriteInColor(string text, System.ConsoleColor consoleColor)
    {
        Console.ForegroundColor = consoleColor;
        Console.WriteLine(text);
        Console.ResetColor();
    }


    public static void WriteSize<T>(T item)
    {
        var serialized = JsonSerializer.Serialize(item);
        int byteCount = Encoding.UTF8.GetByteCount(serialized);

        // Convert bytes to kilobytes (1 KB = 1024 bytes)
        double sizeInKb = byteCount / 1024.0;

        WriteIndented($"Size: {serialized.Length} chars -- {sizeInKb:F2} kb");
    }

    public static void WriteHeader(string text) => WriteInColor($"\n=== {text} ===", ConsoleColor.Cyan);
    public static void WriteSection(string title, ConsoleColor color = ConsoleColor.Yellow) => WriteInColor($"\n-- {title} --", color);
    public static void WriteIndented(string text, int indent = 2) => Console.WriteLine($"{new string(' ', indent)}{text}");
}