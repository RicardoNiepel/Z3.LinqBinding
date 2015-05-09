using System.Collections.Generic;
using System.Linq.Expressions;

namespace Z3.LinqBinding
{
    public interface ITheoremGlobalRewriter
    {
        IEnumerable<LambdaExpression> Rewrite(IEnumerable<LambdaExpression> constraints);
    }
}