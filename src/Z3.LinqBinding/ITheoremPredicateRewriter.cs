using System.Linq.Expressions;

namespace Z3.LinqBinding
{
    public interface ITheoremPredicateRewriter
    {
        MethodCallExpression Rewrite(MethodCallExpression call);
    }
}