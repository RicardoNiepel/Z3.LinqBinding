using System;
using System.Diagnostics;
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


            // Sudoku Extension Usage (Z3.LinqBinding.Sudoku)
            using (var ctx = new Z3Context())
            {
                var theorem = SudokuAsArray
                   .Parse("9.2..54.31...63.255.84.7.6..263.9..1.57.1.29..9.67.53.24.53.6..7.52..3.4.8..4195.") // Very easy
                   .CreateTheorem(ctx);
                var result = theorem.Solve();
                Console.WriteLine(result);
                theorem = SudokuAsArray
                   .Parse("..48...1767.9.....5.8.3...43..74.1...69...78...1.69..51...8.3.6.....6.9124...15..") // Easy
                   .CreateTheorem(ctx);
                result = theorem.Solve();
                Console.WriteLine(result);
                theorem = SudokuAsArray
                   .Parse("..6.......8..542...4..9..7...79..3......8.4..6.....1..2.3.67981...5...4.478319562") // Medium
                   .CreateTheorem(ctx);
                result = theorem.Solve();
                Console.WriteLine(result);
                theorem = SudokuAsArray
                   .Parse("....9.4.8.....2.7..1.7....32.4..156...........952..7.19....5.1..3.4.....1.2.7....") // Hard
                   .CreateTheorem(ctx);
                result = theorem.Solve();
                Console.WriteLine(result);

            }



            // Solving Canibals & Missionaires

            using (var ctx = new Z3Context())
            {
                var can = new MissionariesAndCannibals() { NbMissionaries = 3, SizeBoat = 2, Length = 50 };
                var startTime = stopwatch.Elapsed;
                var minimal = MissionariesAndCannibals.SearchMinimal(can);
                var endTime = stopwatch.Elapsed;
                Console.WriteLine("Minimal Solution to missionaries and cannibals through Binary search");
                Console.WriteLine(minimal == null ? "none" : minimal.ToString());
                Console.WriteLine($"Time to solve: {endTime - startTime}");

                var theorem = can.Create(ctx);
                startTime = stopwatch.Elapsed;
                minimal = theorem.Optimize(Optimization.Minimize, objMnC => objMnC.Length);
                endTime = stopwatch.Elapsed;
                Console.WriteLine("Minimal Solution to missionaries and cannibals through Z3 optimization");
                Console.WriteLine(minimal == null ? "none" : minimal.ToString());
                Console.WriteLine($"Time to solve: {endTime - startTime}");

            }

            // Testing simplification

            using (var ctx = new Z3Context())
            {
                //ctx.Log = Console.Out;
                var can = new MissionariesAndCannibals() { NbMissionaries = 3, SizeBoat = 2, Length = 30 };

                Console.WriteLine($"Non simplified version");
                Console.WriteLine();
                var theorem = can.Create(ctx);
                theorem.SimplifyLambdas = false;
                var startTime = stopwatch.Elapsed;
                var minimal = theorem.Optimize(Optimization.Minimize, objMnC => objMnC.Length);
                var endTime = stopwatch.Elapsed;
                Console.WriteLine("Minimal Solution to missionaries and cannibals non simplified through Z3 optimization");
                Console.WriteLine($"Time to solve: {endTime - startTime}");
                Console.WriteLine($"simplified version");
                Console.WriteLine();
                theorem.SimplifyLambdas = true;
                startTime = stopwatch.Elapsed;
                minimal = theorem.Optimize(Optimization.Minimize, objMnC => objMnC.Length);
                endTime = stopwatch.Elapsed;
                Console.WriteLine("Minimal Solution to missionaries and cannibals simplified through Z3 optimization");
                Console.WriteLine($"Time to solve: {endTime - startTime}");
            }


            //AllSamplesInSameContext();

            Console.Read();


        }




        private static void AllSamplesInSameContext()
        {
            // All samples
            using (var ctx = new Z3Context())
            {
                ctx.Log = Console.Out; // see internal logging

                Print(from t in ctx.NewTheorem(new
                {
                    x = default(bool)
                })

                      where t.x && !t.x
                      select t);

                Print(from t in ctx.NewTheorem(new
                {
                    x = default(bool),
                    y = default(bool)
                })

                      where t.x ^ t.y
                      select t);

                Print(from t in ctx.NewTheorem(new
                {
                    x = default(int),
                    y = default(int)
                })

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


                // Sudoku Extension Usage Z3.LinqBinding.Sudoku demonstrating array capabilities

                Print<SudokuAsArray>(SudokuAsArray
                .Parse("9.2..54.31...63.255.84.7.6..263.9..1.57.1.29..9.67.53.24.53.6..7.52..3.4.8..4195.") // Very easy
                .CreateTheorem(ctx));
                Print<SudokuAsArray>(SudokuAsArray
                .Parse("..48...1767.9.....5.8.3...43..74.1...69...78...1.69..51...8.3.6.....6.9124...15..") // Easy
                .CreateTheorem(ctx));
                Print<SudokuAsArray>(SudokuAsArray
                .Parse("..6.......8..542...4..9..7...79..3......8.4..6.....1..2.3.67981...5...4.478319562") // Medium
                .CreateTheorem(ctx));
                Print<SudokuAsArray>(SudokuAsArray
                .Parse("....9.4.8.....2.7..1.7....32.4..156...........952..7.19....5.1..3.4.....1.2.7....") // Hard
                .CreateTheorem(ctx));

                // Solving Canibals & Missionaires
                var can = new MissionariesAndCannibals() { NbMissionaries = 3, SizeBoat = 2, Length = 50 };
                Print<MissionariesAndCannibals>(can.Create(ctx));
            }
        }



        private static void Print<T>(Theorem<T> t) where T : class
        {
            Console.WriteLine(t);
            var startTime = stopwatch.Elapsed;
            var res = t.Solve();
            var endTime = stopwatch.Elapsed;
            Console.WriteLine(res == null ? "none" : res.ToString());
            Console.WriteLine($"Time to solve: {endTime - startTime}");
            Console.WriteLine();
        }

        private static Stopwatch stopwatch = Stopwatch.StartNew();

    }
}
