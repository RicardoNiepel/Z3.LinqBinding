using System;
using Z3.LinqBinding;
using Z3.LinqBinding.Sudoku;

namespace Z3.LinqBindingDemo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Basic Usage
            using (var ctx = new Z3Context())
            {
                ctx.Log = Console.Out; // see internal logging

                var theorem = from t in ctx.NewTheorem(new { x = default(bool), y = default(bool) })
                              where t.x ^ t.y
                              select t;

                var result = theorem.Solve();
                Console.WriteLine(result);
            }

            // Advanced Usage
            using (var ctx = new Z3Context())
            {
                ctx.Log = Console.Out; // see internal logging

                var theorem = from t in ctx.NewTheorem<Symbols<int, int>>()
                              where t.X1 < t.X2 + 1
                              where t.X1 > 2
                              where t.X1 != t.X2
                              select t;

                var result = theorem.Solve();
                Console.WriteLine(result);
            }

            // Sudoku Extension Usage (Z3.LinqBinding.Sudoku)
            using (var ctx = new Z3Context())
            {
                var theorem = from t in SudokuTheorem.Create(ctx)
                              where t.Cell13 == 2 && t.Cell16 == 1 && t.Cell18 == 6
                              where t.Cell23 == 7 && t.Cell26 == 4
                              where t.Cell31 == 5 && t.Cell37 == 9
                              where t.Cell42 == 1 && t.Cell44 == 3
                              where t.Cell51 == 8 && t.Cell55 == 5 && t.Cell59 == 4
                              where t.Cell66 == 6 && t.Cell68 == 2
                              where t.Cell73 == 6 && t.Cell79 == 7
                              where t.Cell84 == 8 && t.Cell87 == 3
                              where t.Cell92 == 4 && t.Cell94 == 9 && t.Cell97 == 2
                              select t;

                var result = theorem.Solve();
                Console.WriteLine(result);
            }

            // All samples
            using (var ctx = new Z3Context())
            {
                ctx.Log = Console.Out; // see internal logging

                Print(from t in ctx.NewTheorem(new { x = default(bool) })
                      where t.x && !t.x
                      select t);

                Print(from t in ctx.NewTheorem(new { x = default(bool), y = default(bool) })
                      where t.x ^ t.y
                      select t);

                Print(from t in ctx.NewTheorem(new { x = default(int), y = default(int) })
                      where t.x < t.y + 1
                      where t.x > 2
                      select t);

                Print(from t in ctx.NewTheorem<Symbols<int, int>>()
                      where t.X1 < t.X2 + 1
                      where t.X1 > 2
                      where t.X1 != t.X2
                      select t);

                Print(from t in ctx.NewTheorem<Symbols<int, int, int, int, int>>()
                      where t.X1 - t.X2 >= 1
                      where t.X1 - t.X2 <= 3
                      where t.X1 == (2 * t.X3) + t.X5
                      where t.X3 == t.X5
                      where t.X2 == 6 * t.X4
                      select t);

                Print(from t in ctx.NewTheorem<Symbols<int, int>>()
                      where Z3Methods.Distinct(t.X1, t.X2)
                      select t);

                Print(from t in SudokuTheorem.Create(ctx)
                      where t.Cell13 == 2 && t.Cell16 == 1 && t.Cell18 == 6
                      where t.Cell23 == 7 && t.Cell26 == 4
                      where t.Cell31 == 5 && t.Cell37 == 9
                      where t.Cell42 == 1 && t.Cell44 == 3
                      where t.Cell51 == 8 && t.Cell55 == 5 && t.Cell59 == 4
                      where t.Cell66 == 6 && t.Cell68 == 2
                      where t.Cell73 == 6 && t.Cell79 == 7
                      where t.Cell84 == 8 && t.Cell87 == 3
                      where t.Cell92 == 4 && t.Cell94 == 9 && t.Cell97 == 2
                      select t);
            }


          Console.Read();

        }

        private static void Print<T>(Theorem<T> t) where T : class
        {
            Console.WriteLine(t);
            var res = t.Solve();
            Console.WriteLine(res == null ? "none" : res.ToString());
            Console.WriteLine();
        }

      
    }
}