namespace Rechner
{
    internal class Parser
    {
        public Dictionary<TextSpan, string> diagnostics = new();
        private int current = 0;
        private readonly List<Token> tokens;
        public Dictionary<string, double> Vars { get; }
        public Parser(List<Token> _tokens, Dictionary<TextSpan, string> _diagnostics, Dictionary<string, double> _vars)
        {
            tokens = _tokens.FindAll(t => t.Kind != TokenKind.Space);
            Tokens = _tokens;
            diagnostics = _diagnostics;
            Vars = _vars;
        }

        public Expr ParseExpr(ushort pred = 0)
        {
            Token t = Advance();

            try
            {
                return t.Kind switch
                {
                    TokenKind.Number => CheckExtension(new LiteralExpr(t.Literal ?? 0), pred),
                    TokenKind.Identifier => CheckExtension(GetName(t), pred),
                    TokenKind.Minus => CheckExtension(new UnaryExpr(ParseExpr(4), TokenKind.Minus), pred),
                    TokenKind.LParen => CheckExtension(GetGrouping(), pred),
                    TokenKind.Del => GetDel(),
                    _ => Err(t.Span)
                };
            }
            catch { return new ErrorExpr(); }
        }

        private Expr CheckExtension(Expr left, ushort pred)
        {
            Token t = Peek;
            if (t.Kind == TokenKind.Eof)
                return left;

            if (BinPrecedence(t.Kind) > 0)
            {
                ushort precedence;
                Token op = Peek;
                do
                {
                    precedence = BinPrecedence(op.Kind);
                    if (precedence <= pred)
                        break;

                    op = Advance();
                    Expr right = ParseExpr(precedence);
                    left = CheckExtension(new BinaryExpr(left, op.Kind, right), precedence);
                } while (!IsAtEnd && BinPrecedence(Peek.Kind) > 0);
            }
            else if (t.Kind == TokenKind.LParen)
            {
                if (left is NameExpr l)
                {
                    Expr arg = ParseExpr();
                    if (Peek.Kind != TokenKind.RParen)
                        return Err(t.Span, "Erwartete ')'.");

                    Advance();
                    left = new CallExpr(l.Name, arg);
                }
                else
                {
                    Advance();
                    left = new BinaryExpr(left, TokenKind.Star, GetGrouping());
                }
            }
            else if (t.Kind == TokenKind.Bang)
            {
                double l = left.Calc();
                if (l == Math.Round(l) && l >= 0)
                    return new UnaryExpr(left, TokenKind.Bang);
                else return Err(t.Span, "Die Fakultät kann nur auf natürliche Zahlen angewendet werden.");
            }
            else if (t.Kind == TokenKind.Eq)
            {
                if (left is NameExpr l)
                {
                    Advance();
                    Expr value = ParseExpr();
                    if (Vars.ContainsKey(l.Name) || BuiltIn.Vars.ContainsKey(l.Name))
                        return Err(Peek.Span, $"\"{l.Name}\" existiert bereits.");

                    Vars.Add(l.Name, value.Calc());
                    return new VarExpr(l.Name, value);
                }
                else
                    return Err(t.Span, "Variablen brauchen einen Namen.");
            }

            return left;
        }

        private ErrorExpr Err(TextSpan span, string msg = "Ungültiger Ausdruck.")
        {
            Error(span, msg);
            return new ErrorExpr();
        }

        public Expr GetGrouping()
        {
            Expr expr = ParseExpr();
            if (Peek.Kind != TokenKind.RParen)
                return Err(Peek.Span, "Erwartete ')'.");

            expr = new GroupingExpr(expr);
            Advance();
            return expr;
        }

        public Expr GetName(Token t)
        {
            if (Peek.Kind == TokenKind.Eq)
                return new NameExpr(t.Lexeme);

            if (!Vars.ContainsKey(t.Lexeme) && !BuiltIn.Vars.ContainsKey(t.Lexeme) && !BuiltIn.Functions.Any(fn => fn.Name == t.Lexeme))
                return Err(t.Span, $"Konnte \"{t.Lexeme}\" nicht finden.");

            if (Peek.Kind != TokenKind.LParen)
            {
                if (BuiltIn.Functions.Any(fn => fn.Name == t.Lexeme))
                    return Err(t.Span, $"\"{t.Lexeme}\" ist eine Funktion - du musst sie aufrufen.");
                return new LiteralExpr(BuiltIn.Vars.TryGetValue(t.Lexeme, out double value) ? value : Vars[t.Lexeme]);
            }
            else
                return new NameExpr(t.Lexeme);
        }

        public Expr GetDel()
        {
            Token id = Advance();
            if (id.Kind != TokenKind.Identifier)
                return Err(id.Span, "Erwartete einen Namen.");
            else if (BuiltIn.Vars.ContainsKey(id.Lexeme))
                return Err(id.Span, $"Kann keine Konstante löschen.");
            else if (BuiltIn.Functions.Any(fn => fn.Name == id.Lexeme))
                return Err(id.Span, $"Kann keine Funktion löschen.");
            else if (!Vars.ContainsKey(id.Lexeme))
                return Err(id.Span, $"Konnte \"{id.Lexeme}\" nicht finden.");

            Vars.Remove(id.Lexeme);
            return new DelExpr(id.Lexeme);
        }

        public bool IsAtEnd => current >= tokens.Count;
        public Token Peek => tokens[current];

        public List<Token> Tokens { get; }

        public Token Advance()
        {
            Token t = Peek;
            ++current;
            return t;
        }

        public void Error(TextSpan span, string msg)
        {
            if (!diagnostics.ContainsKey(span)) diagnostics.Add(span, msg);
            throw new Exception();
        }

        public static ushort BinPrecedence(TokenKind k) => k switch
        {
            TokenKind.Power => 3,
            TokenKind.Star or TokenKind.Slash or TokenKind.Mod => 2,
            TokenKind.Plus or TokenKind.Minus => 1,
            _ => 0,
        };
    }
}