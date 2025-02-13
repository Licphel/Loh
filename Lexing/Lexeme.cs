using Loh.Values;

namespace Loh.Lexing;

public class Lexeme
{

	public string Portion;
	public int Line;
	public Union Value;
	public Token Type;

	public Lexeme(Token type, string portion, Union val, int ln)
	{
		Type = type;
		Portion = portion;
		Value = val;
		Line = ln;
	}

	public override string ToString()
	{
		return $"[{Type} {Portion} {Value}]";
	}

}
