using System.Text;
using KryptonM.IO;
using Loh.Lexing;

namespace Loh.Compile;

public class CompilerDataonly
{

    private readonly List<Lexeme> code;
    private int itrtimes;
    private int pos;
    private bool stopped;

    public CompilerDataonly(List<Lexeme> _code, int _pos)
    {
        code = _code;
        pos = _pos;
    }

    public object Parse()
    {
        Lexeme c = code[pos];

        itrtimes++;

        if(itrtimes >= 32767)
        {
            stopped = true;
            throw new Exception("Too many iterations. Dataonly table parser is interrupted!");
        }

        switch(c.Type)
        {
            case Token.Brace1:
                return parseObject();
            case Token.Sqbra1:
                return parseArray();
            case Token.String:
            case Token.Null:
            case Token.Number:
                ++pos;
                return c.Value.Boxed;
            case Token.True:
                ++pos;
                return true;
            case Token.False:
                ++pos;
                return false;
            default:
                return null;
        }
    }

    private IBinaryCompound parseObject()
    {
        IBinaryCompound result = IBinaryCompound.New();

        ++pos;

        Lexeme ch = code[pos];

        while(ch.Type != Token.Brace2 && !stopped)
        {
            var key = parseKey();
            ++pos;

            var value = Parse();
            result.Set(key, value);

            ch = code[pos];

            if(ch.Type == Token.Comma)
                ++pos;

            ch = code[pos];
        }

        ++pos;
        return result;
    }

    private IBinaryList parseArray()
    {
        IBinaryList result = IBinaryList.New();
        ++pos;

        Lexeme ch = code[pos];

        while(ch.Type != Token.Sqbra2 && !stopped)
        {
            var value = Parse();
            result.Insert(value);

            ch = code[pos];

            if(ch.Type == Token.Comma)
                ++pos;

            ch = code[pos];
        }

        ++pos;

        return result;
    }

    private string parseKey()
    {
        StringBuilder result = new StringBuilder();

        Lexeme ch = code[pos];

        while(ch.Type != Token.EqAssign && !stopped)
        {
            result.Append(ch.Portion);
            ++pos;
            ch = code[pos];
        }

        return result.ToString();
    }

}