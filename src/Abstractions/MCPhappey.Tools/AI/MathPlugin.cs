
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.AI;

public static class MathPlugin
{
    [Description("Take the square root of a number.")]
    [McpServerTool(Name = "MathPlugin_Sqrt", ReadOnly = true)]
    public static double MathPlugin_Sqrt(
       [Description("The number to take a square root of")] double number1
   )
    {
        return Math.Sqrt(number1);
    }

    [Description("Add two numbers.")]
    [McpServerTool(Name = "MathPlugin_Add", ReadOnly = true)]
    public static double MathPlugin_Add(
        [Description("The first number to add")] double number1,
        [Description("The second number to add")] double number2
    )
    {
        return number1 + number2;
    }

    [Description("Subtract two numbers.")]
    [McpServerTool(Name = "MathPlugin_Subtract", ReadOnly = true)]
    public static double MathPlugin_Subtract(
        [Description("The first number to subtract from")] double number1,
        [Description("The second number to subtract away")] double number2
    )
    {
        return number1 - number2;
    }

    [Description("Multiply two numbers. When increasing by a percentage, don't forget to add 1 to the percentage.")]
    [McpServerTool(Name = "MathPlugin_Multiply", ReadOnly = true)]
    public static double MathPlugin_Multiply(
        [Description("The first number to multiply")] double number1,
        [Description("The second number to multiply")] double number2
    )
    {
        return number1 * number2;
    }

    [Description("Divide two numbers.")]
    [McpServerTool(Name = "MathPlugin_Divide", ReadOnly = true)]
    public static double MathPlugin_Divide(
        [Description("The first number to divide from")] double number1,
        [Description("The second number to divide by")] double number2
    )
    {
        return number1 / number2;
    }

    [Description("Raise a number to a power.")]
    [McpServerTool(Name = "MathPlugin_Power", ReadOnly = true)]
    public static double MathPlugin_Power(
        [Description("The number to raise")] double number1,
        [Description("The power to raise the number to")] double number2
    )
    {
        return Math.Pow(number1, number2);
    }

    [Description("Take the log of a number.")]
    [McpServerTool(Name = "MathPlugin_Log", ReadOnly = true)]
    public static double MathPlugin_Log(
        [Description("The number to take the log of")] double number1,
        [Description("The base of the log")] double number2
    )
    {
        return Math.Log(number1, number2);
    }

    [Description("Round a number to the target number of decimal places.")]
    [McpServerTool(Name = "MathPlugin_Round", ReadOnly = true)]
    public static double MathPlugin_Round(
        [Description("The number to round")] double number1,
        [Description("The number of decimal places to round to")] double number2
    )
    {
        return Math.Round(number1, (int)number2);
    }

    [Description("Take the absolute value of a number.")]
    [McpServerTool(Name = "MathPlugin_Abs", ReadOnly = true)]
    public static double MathPlugin_Abs(
        [Description("The number to take the absolute value of")] double number1
    )
    {
        return Math.Abs(number1);
    }

    [Description("Take the floor of a number.")]
    [McpServerTool(Name = "MathPlugin_Floor", ReadOnly = true)]
    public static double MathPlugin_Floor(
        [Description("The number to take the floor of")] double number1
    )
    {
        return Math.Floor(number1);
    }

    [Description("Take the ceiling of a number.")]
    [McpServerTool(Name = "MathPlugin_Ceiling", ReadOnly = true)]
    public static double MathPlugin_Ceiling(
        [Description("The number to take the ceiling of")] double number1
    )
    {
        return Math.Ceiling(number1);
    }

    [Description("Take the sine of a number.")]
    [McpServerTool(Name = "MathPlugin_Sin", ReadOnly = true)]
    public static double MathPlugin_Sin(
        [Description("The number to take the sine of")] double number1
    )
    {
        return Math.Sin(number1);
    }

    [Description("Take the cosine of a number.")]
    [McpServerTool(Name = "MathPlugin_Cos", ReadOnly = true)]
    public static double MathPlugin_Cos(
        [Description("The number to take the cosine of")] double number1
    )
    {
        return Math.Cos(number1);
    }

    [Description("Take the tangent of a number.")]
    [McpServerTool(Name = "MathPlugin_Tan", ReadOnly = true)]
    public static double MathPlugin_Tan(
        [Description("The number to take the tangent of")] double number1
    )
    {
        return Math.Tan(number1);
    }

    [Description("Take the arcsine of a number.")]
    [McpServerTool(Name = "MathPlugin_Asin", ReadOnly = true)]
    public static double MathPlugin_Asin(
        [Description("The number to take the arcsine of")] double number1
    )
    {
        return Math.Asin(number1);
    }

    [Description("Take the arccosine of a number.")]
    [McpServerTool(Name = "MathPlugin_Acos", ReadOnly = true)]
    public static double MathPlugin_Acos(
        [Description("The number to take the arccosine of")] double number1
    )
    {
        return Math.Acos(number1);
    }

    [Description("Take the arctangent of a number.")]
    [McpServerTool(Name = "MathPlugin_Atan", ReadOnly = true)]
    public static double MathPlugin_Atan(
        [Description("The number to take the arctangent of")] double number1
    )
    {
        return Math.Atan(number1);
    }
}