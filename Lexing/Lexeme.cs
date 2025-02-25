using Loh.Values;

namespace Loh.Lexing;

public class Lexeme
{

    public int Line;

    public string Portion;
    public Token Type;
    public Union Value;

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