namespace Rechner
{
    internal class Parser
    {
        public Dictionary<TextSpan, string> diagnostics = new();
        public readonly Env env = new();
        private int current = 0;
        private readonly List<Token> tokens;
        public Parser(List<Token> _tokens, Dictionary<TextSpan, string> _diagnostics)
        {
            tokens = _tokens.FindAll(t => t.Kind != TokenKind.Space);
            diagnostics = _diagnostics;
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
                    _ => Err(t.Span)
                };
            }
            catch { return new ErrorExpr(); }
        }

        private Expr CheckExtension(Expr left, ushort pred)
        {
            Token t = Peek;
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
                    return new CallExpr(l.Name, arg, env);
                }
                else return Err(t.Span, "Funktionsaufruf ist nur bei einer Namensnennung möglich.");
            }
            else if (t.Kind == TokenKind.Bang)
            {
                double l = left.Calc();
                if (l == Math.Round(l))
                    left = new UnaryExpr(left, TokenKind.Bang);
                else return Err(t.Span, ".");
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

            return new GroupingExpr(expr);
        }

        public Expr GetName(Token t)
        {
            if (!env.Vars.ContainsKey(t.Lexeme) && !env.Functions.Any(fn => fn.Name == t.Lexeme))
                return Err(t.Span, $"Konnte \"{t.Lexeme}\" nicht finden.");
            return new NameExpr(t.Lexeme, env);
        }

        public bool IsAtEnd => current >= tokens.Count;
        public Token Peek => tokens[current];
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