using Loh.Values;

namespace Loh.Lexing;

public unsafe class Lexer
{

	List<Lexeme> Result = new List<Lexeme>();
	int Start = 0, Current = 0, Line = 1;
	string Src;
	bool End => Current >= Src.Length;
	string FileName;

	public List<Lexeme> Analyse(string filename, string source)
	{
		FileName = filename;
		Src = source;

		LohException.PreCrash(() => FileName, () => Line);

		while(!End)
		{
			Start = Current;
			DoNext();
		}

		Result.Add(new Lexeme(Token.Eof, null, Union.Null, Line));

		return Result;
	}

	// Comments are '--', '//', '#' and also, '@' means one-word hint like get_value(@int index).
	void DoNext()
	{
		char ch = Src[Current++];

		switch(ch)
		{
			case '(':
				Push(Token.Paren1);
				break;
			case ')':
				Push(Token.Paren2);
				break;
			case '{':
				Push(Token.Brace1);
				break;
			case '}':
				Push(Token.Brace2);
				break;
			case '[':
				Push(Token.Sqbra1);
				break;
			case ']':
				Push(Token.Sqbra2);
				break;
			case ':':
				Push(Token.Colon);
				break;
			case ',':
				Push(Token.Comma);
				break;
			case '.':
				Push(Token.Dot);
				break;
			case '-':
				if(Match('-'))
				{
					while(Peek() != '\n' && !End) Current++;
					Current++;
					break;
				}
				Push(Token.Minus);
				break;
			case '+':
				Push(Token.Plus);
				break;
			case '*':
				Push(Token.Star);
				break;
			case '/':
				Push(Token.Slash);
				break;
			case '!':
				Push(Match('=') ? Token.NotEq : Token.Exclam);
				break;
			case '>':
				Push(Match('=') ? Token.GreatEq : Token.Greater);
				break;
			case '<':
				Push(Match('=') ? Token.LessEq : Token.Less);
				break;
			case '=':
				Push(Match('=') ? Token.Equal : Token.EqAssign);
				break;
			case '%':
				Push(Token.Mod);
				break;
			case '^':
				Push(Token.Power);
				break;
			case '@':
				while(!char.IsWhiteSpace(Peek()) && Peek() != '\n' && !End) Current++;
				break;
			case ';':
				if(PeekNext() == '\n')
					break;
				else
					throw LohException.Disassembling($"Semicolon should be at the end of a line, or delete it!");
			case ' ':
			case '\t':
			case '\r':
				break;
			case '\n':
				Line++;
				break;
			case '"':
				PickString();
				break;
			default:
				if(IsDigit(ch))
					PickNum();
				else if(IsAlpbtc(ch))
					PickIdent();
				else
					throw LohException.Disassembling($"Unidentifiable lexeme '{ch}'.");
				break;
		}
	}

	bool IsIdent(char ch) { return IsDigit(ch) || IsAlpbtc(ch); }

	bool IsDigit(char ch) { return ch >= '0' && ch <= '9'; }

	bool IsAlpbtc(char ch) { return ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch == '$' || ch == '#'; }

	void Push(Token type) { Push(type, Union.Null); }

	void Push(Token type, Union v)
	{
		string txt = Src.Substring(Start, Current - Start);
		Result.Add(new Lexeme(type, txt, v, Line));
	}

	bool Match(char exp)
	{
		if(End)
			return false;
		if(Src[Current] != exp)
			return false;
		Current++;
		return true;
	}

	char Peek()
	{
		if(End)
			return '\0';
		return Src[Current];
	}

	char PeekNext()
	{
		if(Current + 1 >= Src.Length)
			return '\0';
		return Src[Current + 1];
	}

	void PickString()
	{
		while(Peek() != '"' && !End)
		{
			if(Peek() == '\n') Line++;
			Current++;
		}
		if(End)
			throw LohException.Disassembling("Unable to find paired quotation marks for string.");
		Current++;
		string sub = Src.Substring(Start + 1, Current - Start - 2);
		Push(Token.String, Union.GetFromObject(sub));
	}

	void PickNum()
	{
		while(IsDigit(Peek()) || Peek() == '-')
			Current++;
		if(Peek() == '.' && IsDigit(PeekNext()))
		{
			Current++;
			while(IsDigit(Peek()))
				Current++;
		}
		string sub = Src.Substring(Start, Current - Start);
		Push(Token.Number, new Union(LayeredSeek(sub)));
		return;

		float LayeredSeek(string code)
		{
			if(float.TryParse(code, out float r))
				return r;
			throw LohException.Disassembling($"Unknown value type '{code}'.");
		}
	}

	void PickIdent()
	{
		while(IsIdent(Peek()))
			Current++;
		string sub = Src.Substring(Start, Current - Start);
		Push(GetTypeFromString(sub));
	}

	Token GetTypeFromString(string s)
	{
		return Tokens.GetValueOrDefault(s, Token.Ident);
	}

	static Dictionary<string, Token> Tokens = new();

	static Lexer()
	{
		foreach(Token token in Enum.GetValues(typeof(Token)))
		{
			Tokens[token.ToString().ToLower()] = token;
		}
	}

}
