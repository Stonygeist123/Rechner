using System.Globalization;

namespace Rechner
{
    internal class Program
    {
        static void Main()
        {
            Repl repl = new();
            Dictionary<string, double> vars = new();

            while (true)
            {
                string src = repl.Read();
                Lexer lexer = new(src);
                List<Token> tokens = lexer.Lex();

                Parser parser = new(tokens, lexer.diagnostics, vars);
                Expr expr = parser.ParseExpr();

                if (parser.diagnostics.Any())
                {
                    repl.Print();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    foreach (KeyValuePair<TextSpan, string> d in parser.diagnostics)
                        Console.WriteLine($"{new string('-', d.Key.Start + 3)}{new string('^', d.Key.Length)} {d.Value}");

                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(Math.Round(expr.Calc(), 15).ToString(CultureInfo.InvariantCulture));
                    Console.ResetColor();
                }
            }
        }
    }
}