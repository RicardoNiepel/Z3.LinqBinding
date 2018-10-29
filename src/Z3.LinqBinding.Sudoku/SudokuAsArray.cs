using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Z3.LinqBinding.Sudoku
{

   /// <summary>
   /// Class that represents a Sudoku, fully or partially completed
   /// Holds a list of 81 int for cells, with 0 for empty cells
   /// Can parse strings and files from most common formats and displays the sudoku in an easy to read format
   /// </summary>
   public class SudokuAsArray
   {

       private static readonly int[] Indices = Enumerable.Range(0, 9).ToArray();

      // The List property makes it easier to manipulate cells,
      public List<int> Cells { get; set; } = Enumerable.Repeat(0, 81).ToList();

      ///// <summary>
      ///// The array property is to be used in linq to Z3
      ///// </summary>
      //public int[] Cells
      //{
      //   get => CellsList.ToArray();
      //   set => CellsList = new List<int>(value);
      //}

      /// <summary>
      /// Creates a Z3 theorem to solve the sudoku, adding the general constraints, and the mask constraints for this particular Sudoku
      /// </summary>
      /// <param name="context">The linq to Z3 context wrapping Z3</param>
      /// <returns>a theorem with all constraints compounded</returns>
      public Theorem<SudokuAsArray> CreateTheorem(Z3Context context)
      {
         var toReturn = Create(context);
         for (int i = 0; i < 81; i++)
         {
            if (Cells[i] != 0)
            {
               var idx = i;
               var cellValue = Cells[i];
               toReturn = toReturn.Where(sudoku => sudoku.Cells[idx] == cellValue);
            }
         }

         return toReturn;

      }


      /// <summary>
      /// Creates a Z3-capable theorem to solve a Sudoku
      /// </summary>
      /// <param name="context">The wrapping Z3 context used to interpret c# Lambda into Z3 constraints</param>
      /// <returns>A typed theorem to be further filtered with additional contraints</returns>
      public static Theorem<SudokuAsArray> Create(Z3Context context)
      {

         var sudokuTheorem = context.NewTheorem<SudokuAsArray>();

         // Cells have values between 1 and 9
         for (int i = 0; i < 9; i++)
         {
            for (int j = 0; j < 9; j++)
            {
               //To avoid side effects with lambdas, we copy indices to local variables
               var i1 = i;
               var j1 = j;
               sudokuTheorem = sudokuTheorem.Where(sudoku => (sudoku.Cells[i1 * 9 + j1] > 0 && sudoku.Cells[i1 * 9 + j1] < 10));
            }
         }

         // Rows must have distinct digits
         for (int r = 0; r < 9; r++)
         {
            //Again we avoid Lambda closure side effects
            var r1 = r;
                sudokuTheorem = sudokuTheorem.Where(t => Z3Methods.Distinct(Indices.Select(j => t.Cells[r1 * 9 + j]).ToArray()));

            }

         // Columns must have distinct digits
         for (int c = 0; c < 9; c++)
         {
            //Preventing closure side effects
            var c1 = c;
                sudokuTheorem = sudokuTheorem.Where(t => Z3Methods.Distinct(Indices.Select(i => t.Cells[i * 9 + c1]).ToArray()));
            }

         // Boxes must have distinct digits
         for (int b = 0; b < 9; b++)
         {
            //On évite les effets de bords par closure
            var b1 = b;
            // We retrieve to top left cell for all boxes, using integer division and remainders.
            var iStart = b1 / 3;
            var jStart = b1 % 3;
            var indexStart = iStart * 3 * 9 + jStart * 3;
            sudokuTheorem = sudokuTheorem.Where(t => Z3Methods.Distinct(new int[]
                  {
                     t.Cells[indexStart ],
                     t.Cells[indexStart+1],
                     t.Cells[indexStart+2],
                     t.Cells[indexStart+9],
                     t.Cells[indexStart+10],
                     t.Cells[indexStart+11],
                     t.Cells[indexStart+18],
                     t.Cells[indexStart+19],
                     t.Cells[indexStart+20],
                  }
               )
            );
         }

         return sudokuTheorem;
      }



      /// <summary>
      /// Displays a Sudoku in an easy-to-read format
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         var lineSep = new string('-', 31);
         var blankSep = new string(' ', 8);

         var output = new StringBuilder();
         output.Append(lineSep);
         output.AppendLine();

         for (int row = 1; row <= 9; row++)
         {
            output.Append("| ");
            for (int column = 1; column <= 9; column++)
            {

               var value = Cells[(row - 1) * 9 + (column - 1)];

               output.Append(value);
               if (column % 3 == 0)
               {
                  output.Append(" | ");
               }
               else
               {
                  output.Append("  ");
               }
            }

            output.AppendLine();
            if (row % 3 == 0)
            {
               output.Append(lineSep);
            }
            else
            {
               output.Append("| ");
               for (int i = 0; i < 3; i++)
               {
                  output.Append(blankSep);
                  output.Append("| ");
               }
            }
            output.AppendLine();
         }

         return output.ToString();
      }

      /// <summary>
      /// Parses a single Sudoku
      /// </summary>
      /// <param name="sudokuAsString">the string representing the sudoku</param>
      /// <returns>the parsed sudoku</returns>
      public static SudokuAsArray Parse(string sudokuAsString)
      {
         return ParseMulti(new[] { sudokuAsString })[0];
      }

      /// <summary>
      /// Parses a file with one or several sudokus
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns>the list of parsed Sudokus</returns>
      public static List<SudokuAsArray> ParseFile(string fileName)
      {
         return ParseMulti(File.ReadAllLines(fileName));
      }

      /// <summary>
      /// Parses a list of lines into a list of sudoku, accounting for most cases usually encountered
      /// </summary>
      /// <param name="lines">the lines of string to parse</param>
      /// <returns>the list of parsed Sudokus</returns>
      public static List<SudokuAsArray> ParseMulti(string[] lines)
      {
         var toReturn = new List<SudokuAsArray>();
         var cells = new List<int>(81);
         foreach (var line in lines)
         {
            if (line.Length > 0)
            {
               if (char.IsDigit(line[0]) || line[0] == '.' || line[0] == 'X' || line[0] == '-')
               {
                  foreach (char c in line)
                  {
                     int? cellToAdd = null;
                     if (char.IsDigit(c))
                     {
                        var cell = (int)Char.GetNumericValue(c);
                        cellToAdd = cell;
                     }
                     else
                     {
                        if (c == '.' || c == 'X' || c == '-')
                        {
                           cellToAdd = 0;
                        }
                     }

                     if (cellToAdd.HasValue)
                     {
                        cells.Add(cellToAdd.Value);
                        if (cells.Count == 81)
                        {
                           toReturn.Add(new SudokuAsArray() { Cells = new List<int>(cells) });
                           cells.Clear();
                        }
                     }
                  }
               }
            }
         }

         return toReturn;
      }

   }
}