namespace Loh.Runtime;

public enum VMOP
{

    // Stack op
    StPop,
    StPush,
    StCpy,

    // Cache op
    RePop,
    ReLoad,
    RePush,
    ReStore,

    // Var op
    GetLocal,
    SetLocal,

    GetUpval,
    SetUpval,
    StoUpval,

    GetFixed,
    SetFixed,

    GetTable,
    GetTabKv,
    SetTable,
    GetTabLe,

    GetArray,
    SetArray,

    // Control op
    Jmp,
    Jmpfn,
    Jmpf,
    Jback,

    // Function & Object op
    Call,
    Return,
    Close,
    TabCall,
    Table,
    Array,

    // Comparison op
    Equal,
    NotEq,
    Greater,
    Less,
    GreatEq,
    LessEq,

    // Calculation op
    Neg,
    Not,
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Pow

}