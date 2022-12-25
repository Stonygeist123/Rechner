using System.Globalization;

namespace Rechner
{
    internal class Program
    {
        static void Main()
        {
            Dictionary<string, double> vars = new();
            Dictionary<string, Expr> fns = new();
            Dictionary<uint, string> options = new() { { 1, "Taschenrechner" }, { 2, "Graphen" } };

            string? input = null;
            while (input == null || input.Trim().Length == 0 || !uint.TryParse(input, out uint i) || !options.ContainsKey(i))
            {
                Console.WriteLine($"Gebe die Zahl für die jeweilige Option ein:");
                foreach (KeyValuePair<uint, string> o in options)
                    Console.WriteLine($"[{o.Key}]: {o.Value}");
                input = Console.ReadLine();
                Console.Clear();
            }

            if (uint.Parse(input) == 1)
            {
                while (true)
                {
                    Repl repl = new();
                    string src = repl.Read();
                    Lexer lexer = new(src);
                    List<Token> tokens = lexer.Lex();

                    Parser parser = new(tokens, lexer.diagnostics, vars, fns);
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
                        if (expr is not FnDecl)
                            Console.WriteLine(Math.Round(expr.Calc(), 15).ToString(CultureInfo.InvariantCulture));
                        Console.ResetColor();
                    }
                }
            }
            /* 
    else if (uint.Parse(input) == 2)
    {
        while (true)
        {
            Repl repl = new();
            string src = repl.Read(true);
            Lexer lexer = new(src);
            List<Token> tokens = lexer.Lex();

            Parser parser = new(tokens, lexer.diagnostics, vars, true);
            Expr expr = parser.ParseExpr();

            if (parser.diagnostics.Any())
            {
                repl.Print();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                foreach (KeyValuePair<TextSpan, string> d in parser.diagnostics)
                    Console.WriteLine($"{new string('-', d.Key.Start + 7)}{new string('^', d.Key.Length)} {d.Value}");
                Console.ResetColor();
            }else
             {
                 Console.WriteLine();
                 List<Graph.Point> pointsN = new();
                 List<Graph.Point> pointsP = new();
                 for (int i = 1; i <= 50; ++i)
                 {
                     double c = expr.Calc(-i);
                     pointsN.Add(new Graph.Point(-i, (int)Math.Round(c)));
                 }

                 for (int i = 0; i <= 50; ++i)
                 {
                     double c = expr.Calc(i);
                     pointsP.Add(new Graph.Point(i, (int)Math.Round(c)));
                 }

                 Graph.DrawChart(pointsN.DistinctBy(p => p.X).ToList(), pointsP.DistinctBy(p => p.X).ToList());
             }
        }
        }*/
        }
    }
}