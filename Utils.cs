namespace Rechner
{
    public readonly struct TextSpan
    {
        public int Start { get; }
        public int End { get; }
        public TextSpan(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Length => End - Start;
    }

    public enum TokenKind
    {
        Number,
        Identifier,
        Plus,
        Minus,
        Star,
        Slash,
        Mod,
        Power,
        Bang,
        LParen,
        RParen,
        Comma,
        Space,
        Eof,
        Bad
    }

    public class Token
    {
        public TokenKind Kind { get; }
        public TextSpan Span { get; }
        public string Lexeme { get; }
        public double? Literal { get; }
        public Token(TokenKind kind, TextSpan span, string lexeme, double? literal = null)
        {
            Kind = kind;
            Lexeme = lexeme;
            Literal = literal;
            Span = span;
        }

        public override string ToString() => $"Kind: {Kind}\nSpan: {Span.Start}-{Span.End}\nLexeme: {Lexeme}\n";
    }

    public readonly struct Fn
    {
        public string Name { get; }
        public Func<double, double> Func { get; }
        public Fn(string _name, Func<double, double> _func)
        {
            Func = _func;
            Name = _name;
        }
    }

    public class Env
    {
        public Dictionary<string, double> Vars { get; } = new() { { "pi", Math.PI }, { "e", Math.E } };
        public List<Fn> Functions { get; } = new() {
            new Fn("sin", x => Math.Sin(x * (Math.PI / 180))),
            new Fn("cos", x => Math.Cos(x  * Math.PI / 180.0)),
            new Fn("tan", Math.Tan),
            new Fn("sqrt",  Math.Sqrt),
            new Fn("cbrt",  Math.Cbrt)
        };

        public double GetVariable(string n) => Vars[n];
        public Fn GetFunction(string n) => Functions.Find(fn => fn.Name == n);
    }

    public abstract class Expr
    {
        public abstract double Calc();
        public abstract string Stringify(int indent);
    }

    public class LiteralExpr : Expr
    {
        public double Value { get; }
        public LiteralExpr(double value) => Value = value;
        public override double Calc() => Value;
        public override string Stringify(int indent) => $"{new string(' ', indent)}-> {Value.ToString().Replace(',', '.')}";
    }

    public class UnaryExpr : Expr
    {
        public Expr Operand { get; }
        public TokenKind Op { get; }
        public UnaryExpr(Expr _operand, TokenKind _op)
        {
            Operand = _operand;
            Op = _op;
        }

        public override double Calc() => Op == TokenKind.Minus ? -Operand.Calc() : Enumerable.Range(1, (int)Operand.Calc()).Aggregate(1, (p, i) => p * i);
        public override string Stringify(int indent) => $"{new string(' ', indent)}-> Unary\n{new string(' ', indent + 3)}Operand:\n{Operand.Stringify(indent + 6)}";
    }

    public class BinaryExpr : Expr
    {
        public Expr Left { get; }
        public TokenKind Op { get; }
        public Expr Right { get; }
        public BinaryExpr(Expr _left, TokenKind _op, Expr _right)
        {
            Left = _left;
            Op = _op;
            Right = _right;
        }

        public override double Calc()
        {
            double L = Left.Calc();
            double R = Right.Calc();
            return Op switch
            {
                TokenKind.Plus => L + R,
                TokenKind.Minus => L - R,
                TokenKind.Star => L * R,
                TokenKind.Slash => L / R,
                TokenKind.Power => Math.Pow(L, R),
                TokenKind.Mod => L % R,
                _ => 0
            };
        }
        public override string Stringify(int indent) => $"{new string(' ', indent)}-> Binary\n{new string(' ', indent + 3)}Left:\n{Left.Stringify(indent + 6)}\n{new string(' ', indent + 3)}Op: {Op}\n{new string(' ', indent + 3)}Right:\n{Right.Stringify(indent + 6)}";
    }

    public class GroupingExpr : Expr
    {
        public Expr Expr { get; }
        public GroupingExpr(Expr _expr) => Expr = _expr;
        public override double Calc() => Expr.Calc();
        public override string Stringify(int indent) => $"{new string(' ', indent)}-> Grouping\n{new string(' ', indent + 3)}Expr:\n{Expr.Stringify(indent + 6)}";
    }

    public class NameExpr : Expr
    {
        public string Name { get; }
        public Env Env { get; }
        public NameExpr(string _name, Env _env)
        {
            Name = _name;
            Env = _env;
        }

        public override double Calc() => Env.GetVariable(Name);
        public override string Stringify(int indent) => $"{new string(' ', indent)}-> {Name}";
    }

    public class CallExpr : Expr
    {
        public string Callee { get; }
        public Expr Arg { get; }
        public Env Env { get; }
        public CallExpr(string _callee, Expr _arg, Env _env)
        {
            Callee = _callee;
            Arg = _arg;
            Env = _env;
        }

        public override double Calc() => Env.GetFunction(Callee).Func.Invoke(Arg.Calc());
        public override string Stringify(int indent) => $"{new string(' ', indent)}-> Call\n{new string(' ', indent + 3)}Callee: {Callee}\n{new string(' ', indent + 3)}Arg: {Arg.Stringify(indent + 6)}";
    }

    public class ErrorExpr : Expr
    {
        public override double Calc() => 0;
        public override string Stringify(int indent) => $"{new string(' ', indent)}-> Error";
    }
}
