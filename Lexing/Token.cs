namespace Loh.Lexing;

// Token members.
// Some of them are reserved.
public enum Token
{

    Paren1, // (
    Paren2, // )
    Brace1, // {
    Brace2, // }
    Sqbra1, // [
    Sqbra2, // ]
    Colon, // :
    Comma, // ,
    Dot, // .
    Minus, // -
    Plus, // +
    Slash, // /
    Star, // *
    Mod, // %
    Power, // ^
    Sized, // ~

    Exclam, // !
    NotEq, // !=
    EqAssign, // =
    Equal, // ==
    Greater, // >
    GreatEq, // >=
    Less, // <
    LessEq, // <=

    Ident, // Identifier like "var1".
    String, // String literal.
    Number, // Number of all types.

    Do, // do
    End, // end
    Not, // not
    And, // and
    Or, // or
    Else, // else
    False, // false
    True, // true
    Function, // function
    For, // for
    If, // if
    Return, // return
    Local, // local
    While, // while
    Require, // require
    Native, // native
    Const, // const
    Loop, // loop
    Goto, // goto
    Is, // is
    Null, // null
    As, // as
    Continue, // continue
    Break, // break
    Switch, // switch
    Foreach, // foreach
    In, // in
    Out, // out
    Of, // of
    Define, // define
    Default, // default
    With, // with
    Export, // export
    Dataonly, // dataonly

    Eof // End of the file.

}