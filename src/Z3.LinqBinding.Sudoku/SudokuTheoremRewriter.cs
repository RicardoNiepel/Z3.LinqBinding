using System.Collections.Generic;
using System.Linq.Expressions;

namespace Z3.LinqBinding.Sudoku
{
    public class SudokuTheoremRewriter : ITheoremGlobalRewriter
    {
        public IEnumerable<LambdaExpression> Rewrite(IEnumerable<LambdaExpression> constraints)
        {
            return constraints;
        }
    }
}