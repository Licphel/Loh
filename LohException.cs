using Kinetic;
using Kinetic.App;
using Loh.Runtime;

namespace Loh;

public unsafe class LohException : Exception
{

	public static Factory<string> CurrentFile;
	public static Factory<int> CurrentLine;

	public static void PreCrash(Factory<string> file, Factory<int> line)
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
		return new LohException($"Runtime Exception in {CurrentFile()} at line {CurrentLine()}: {message} (about to run {opnext})");
	}

	public static LohException Runtime(string message)
	{
		return new LohException($"Runtime Exception in {CurrentFile()} at line {CurrentLine()}: {message}");
	}

	LohException(string? message) : base(message)
	{
	}

}
