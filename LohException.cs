using Loh.Runtime;

namespace Loh;

public class LohException : Exception
{

    public static Func<string> CurrentFile;
    public static Func<int> CurrentLine;

    private LohException(string? message) : base(message)
    {
    }

    public static void PreCrash(Func<string> file, Func<int> line)
    {
        CurrentFile = file;
        CurrentLine = line;
    }

    public static LohException Disassembling(string message)
    {
        return new LohException($"Disassembling Exception: {message}");
    }

    public static LohException Lexing(string message)
    {
        return new LohException($"Lexing Exception in {CurrentFile()} at line {CurrentLine()}: {message}");
    }

    public static LohException Compiling(string message)
    {
        return new LohException($"Compling Exception in {CurrentFile()} at line {CurrentLine()}: {message}");
    }

    public static LohException Runtime(string message, VMOP opnext)
    {
        return new LohException(
            $"Runtime Exception in {CurrentFile()} at line {CurrentLine()}: {message} (about to run {opnext})");
    }

    public static LohException Runtime(string message)
    {
        return new LohException($"Runtime Exception in {CurrentFile()} at line {CurrentLine()}: {message}");
    }

}