using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Z3.LinqBinding
{
    /// <summary>
    /// Representation of a theorem with its constraints.
    /// </summary>
    public class Theorem
    {
        /// <summary>
        /// Theorem constraints.
        /// </summary>
        private IEnumerable<LambdaExpression> _constraints;

        /// <summary>
        /// Z3 context under which the theorem is solved.
        /// </summary>
        private Z3Context _context;

        /// <summary>
        /// Creates a new theorem for the given Z3 context.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        protected Theorem(Z3Context context)
            : this(context, new List<LambdaExpression>())
        {
        }

        /// <summary>
        /// Creates a new pre-constrained theorem for the given Z3 context.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        /// <param name="constraints">Constraints to apply to the created theorem.</param>
        protected Theorem(Z3Context context, IEnumerable<LambdaExpression> constraints)
        {
            _context = context;
            _constraints = constraints;
        }

        /// <summary>
        /// Gets the constraints of the theorem.
        /// </summary>
        protected IEnumerable<LambdaExpression> Constraints
        {
            get
            {
                return _constraints;
            }
        }

        /// <summary>
        /// Gets the Z3 context under which the theorem is solved.
        /// </summary>
        protected Z3Context Context
        {
            get
            {
                return _context;
            }
        }

        /// <summary>
        /// Returns a comma-separated representation of the constraints embodied in the theorem.
        /// </summary>
        /// <returns>Comma-separated string representation of the theorem's constraints.</returns>
        public override string ToString()
        {
            return string.Join(", ", (from c in _constraints select c.Body.ToString()).ToArray());
        }

        /// <summary>
        /// Solves the theorem using Z3.
        /// </summary>
        /// <typeparam name="T">Theorem environment type.</typeparam>
        /// <returns>Result of solving the theorem; default(T) if the theorem cannot be satisfied.</returns>
        protected T Solve<T>()
        {
            // TODO: some debugging around issues with proper disposal of native resources…
            // using (Context context = _context.CreateContext())
            Context context = _context.CreateContext();
            {
                var environment = GetEnvironment<T>(context);
                Solver solver = context.MkSimpleSolver();

                AssertConstraints<T>(context, solver, environment);

                //Model model = null;
                //if (context.CheckAndGetModel(ref model) != LBool.True)
                //    return default(T);

                Status status = solver.Check();
                if (status != Status.SATISFIABLE)
                {
                    throw new UnsatisfiableTheoremException();
                }

                return GetSolution<T>(solver.Model, environment);
            }
        }

        /// <summary>
        /// Maps the properties on the theorem environment type to Z3 handles for bound variables.
        /// </summary>
        /// <typeparam name="T">Theorem environment type to create a mapping table for.</typeparam>
        /// <param name="context">Z3 context.</param>
        /// <returns>Environment mapping table from .NET properties onto Z3 handles.</returns>
        protected virtual Environment GetEnvironment<T>(Context context)
        {
            var environment = new Environment();

            //
            // All public properties are considered part of the theorem's environment.
            // Notice we can't require custom attribute tagging if we want the user to be able to
            // use anonymous types as a convenience solution.
            //
            foreach (var parameter in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                //
                // Normalize types when facing Z3. Theorem variable type mappings allow for strong
                // typing within the theorem, while underlying variable representations are Z3-
                // friendly types.
                //
                var parameterType = parameter.PropertyType;
                var parameterTypeMapping = (TheoremVariableTypeMappingAttribute)parameterType.GetCustomAttributes(typeof(TheoremVariableTypeMappingAttribute), false).SingleOrDefault();
                if (parameterTypeMapping != null)
                    parameterType = parameterTypeMapping.RegularType;

                //
                // Map the environment onto Z3-compatible types.
                //
                switch (Type.GetTypeCode(parameterType))
                {
                    case TypeCode.Boolean:
                        //environment.Add(parameter, context.MkConst(parameter.Name, context.MkBoolType()));
                        environment.Add(parameter, context.MkBoolConst(parameter.Name));
                        break;
                    case TypeCode.Int32:
                        //environment.Add(parameter, context.MkConst(parameter.Name, context.MkIntType()));
                        environment.Add(parameter, context.MkIntConst(parameter.Name));
                        break;
                    case TypeCode.Double:
                        environment.Add(parameter, context.MkRealConst(parameter.Name));
                        break;
                    default:
                        throw new NotSupportedException("Unsupported parameter type for " + parameter.Name + ".");
                }
            }

            return environment;
        }

        protected virtual T CreateResultObject<T>()
        {
            Type t = typeof(T);
            if (t.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any())
            {
                // Anonymous types have a constructor that takes in values for all its properties.
                // However, we don't know the order and it's hard to correlate back the parameters
                // to the underlying properties. So, we want to bypass that constructor altogether
                // by using the FormatterServices to create an uninitialized (all-zero) instance.
                return (T)FormatterServices.GetUninitializedObject(t);
            }
            //
            // Straightforward case of having an "onymous type" at hand.
            //
            return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Gets the solution object for the solved theorem.
        /// </summary>
        /// <typeparam name="T">Environment type to create an instance of.</typeparam>
        /// <param name="model">Z3 model to evaluate theorem parameters under.</param>
        /// <param name="environment">Environment with bindings of theorem variables to Z3 handles.</param>
        /// <returns>Instance of the enviroment type with theorem-satisfying values.</returns>
        private T GetSolution<T>(Model model, Environment environment)
        {
            Type t = typeof(T);

            T result = CreateResultObject<T>();
            //
            // Determine whether T is a compiler-generated type, indicating an anonymous type.
            // This check might not be reliable enough but works for now.
            //
            if (t.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any())
            {
                //
                // Here we take advantage of undesirable knowledge on how anonymous types are
                // implemented by the C# compiler. This is risky but we can live with it for
                // now in this POC. Because the properties are get-only, we need to perform
                // nominal matching with the corresponding backing fields.
                //
                var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var parameter in environment.Keys)
                {
                    //
                    // Mapping from property to field.
                    //
                    var field = fields.Where(f => f.Name.StartsWith("<" + parameter.Name + ">")).SingleOrDefault();

                    //
                    // Evaluation of the values though the handle in the environment bindings.
                    //
                    Expr val = model.Eval(environment[parameter]);
                    switch (Type.GetTypeCode(parameter.PropertyType))
                    {
                        case TypeCode.Boolean:
                            field.SetValue(result, val.IsTrue);
                            break;
                        case TypeCode.Double:
                            field.SetValue(result, ((RatNum)val).Double);
                            break;
                        case TypeCode.Int32:
                            field.SetValue(result, ((IntNum)val).Int);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported parameter type for " + parameter.Name + ".");
                    }
                }

                return result;
            }
            else
            {
                foreach (var parameter in environment.Keys)
                {
                    //
                    // Normalize types when facing Z3. Theorem variable type mappings allow for strong
                    // typing within the theorem, while underlying variable representations are Z3-
                    // friendly types.
                    //
                    var parameterType = parameter.PropertyType;
                    var parameterTypeMapping = (TheoremVariableTypeMappingAttribute)parameterType.GetCustomAttributes(typeof(TheoremVariableTypeMappingAttribute), false).SingleOrDefault();
                    if (parameterTypeMapping != null)
                        parameterType = parameterTypeMapping.RegularType;

                    //
                    // Evaluation of the values though the handle in the environment bindings.
                    //
                    Expr val = model.Eval(environment[parameter]);
                    object value;
                    switch (Type.GetTypeCode(parameterType))
                    {
                        case TypeCode.Boolean:
                            value = val.IsTrue;
                            break;
                        case TypeCode.Int32:
                            if (val is IntExpr intExpr)
                            {
                                value = 0;
                                break;
                            }                            
                            value = ((IntNum)val).Int;
                            break;
                        case TypeCode.Double:
                            value = ((RatNum)val).Double;
                            break;
                        default:
                            value = GetSolutionValue(parameter, model, environment);
                            break;
                    }

                    //
                    // If there was a type mapping, we need to convert back to the original type.
                    // In that case we expect a constructor with the mapped type to be available.
                    //
                    if (parameterTypeMapping != null)
                    {
                        var ctor = parameter.PropertyType.GetConstructor(new Type[] { parameterType });
                        if (ctor == null)
                            throw new InvalidOperationException("Could not construct an instance of the mapped type " + parameter.PropertyType.Name + ". No public constructor with parameter type " + parameterType + " found.");

                        value = ctor.Invoke(new object[] { value });
                    }

                    parameter.SetValue(result, value, null);
                }

                return result;
            }
        }

        protected virtual object GetSolutionValue(PropertyInfo parameter, Model model, Environment environment)
        {
            throw new NotSupportedException($"Unsupported parameter type for {parameter.Name} ({Type.GetTypeCode(parameter.PropertyType)})");
        }

        /// <summary>
        /// Visitor method to translate a constant expression.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        /// <param name="constant">Constant expression.</param>
        /// <returns>Z3 expression handle.</returns>
        private static Expr VisitConstant(Context context, ConstantExpression constant)
        {
            switch (Type.GetTypeCode(constant.Type))
            {
                case TypeCode.Boolean:
                    return (bool)constant.Value ? context.MkTrue() : context.MkFalse();
                case TypeCode.Double:
                    var fraction = new Fraction((double)constant.Value);
                    return context.MkReal((int)fraction.Numerator, (int)fraction.Denominator);
                case TypeCode.Int32:
                    return context.MkNumeral((int)constant.Value, context.IntSort);
            }

            throw new NotSupportedException("Unsupported constant type.");
        }

        /// <summary>
        /// Visitor method to translate a member expression.
        /// </summary>
        /// <param name="environment">Environment with bindings of theorem variables to Z3 handles.</param>
        /// <param name="member">Member expression.</param>
        /// <param name="param">Parameter used to express the constraint on.</param>
        /// <returns>Z3 expression handle.</returns>
        private static Expr VisitMember(Environment environment, MemberExpression member, ParameterExpression param)
        {
            //
            // E.g. Symbols l = ...;
            //      theorem.Where(s => l.X1)
            //                         ^^
            /*if (member.Expression != param)
            {
                throw new NotSupportedException("Encountered member access not targeting the constraint parameter.");
            }*/

            //
            // Only members we allow currently are direct accesses to the theorem's variables
            // in the environment type. So we just try to find the mapping from the environment
            // bindings table.
            //
            //PropertyInfo property = member.Member as PropertyInfo;
            if (!environment.TryGetExpression(member, param, out var value))
            {
                //if (property == null || !environment.TryGetValue(property, out value))
                throw new NotSupportedException("Unknown parameter encountered: " + member.Member.Name + ".");
            }

            return value;
        }

        /// <summary>
        /// Asserts the theorem constraints on the Z3 context.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        /// <param name="environment">Environment with bindings of theorem variables to Z3 handles.</param>
        /// <typeparam name="T">Theorem environment type.</typeparam>
        private void AssertConstraints<T>(Context context, Solver solver, Environment environment)
        {
            var constraints = _constraints;

            //
            // Global rewriter registered?
            //
            var rewriterAttr = (TheoremGlobalRewriterAttribute)typeof(T).GetCustomAttributes(typeof(TheoremGlobalRewriterAttribute), false).SingleOrDefault();
            if (rewriterAttr != null)
            {
                //
                // Make sure the specified rewriter type implements the ITheoremGlobalRewriter.
                //
                var rewriterType = rewriterAttr.RewriterType;
                if (!typeof(ITheoremGlobalRewriter).IsAssignableFrom(rewriterType))
                    throw new InvalidOperationException("Invalid global rewriter type definition. Did you implement ITheoremGlobalRewriter?");

                //
                // Assume a parameterless public constructor to new up the rewriter.
                //
                var rewriter = (ITheoremGlobalRewriter)Activator.CreateInstance(rewriterType);

                //
                // Do the rewrite.
                //
                constraints = rewriter.Rewrite(constraints);
            }

            //
            // Visit, assert and log.
            //
            foreach (var constraint in constraints)
            {
                BoolExpr c = (BoolExpr)Visit(context, environment, constraint.Body, constraint.Parameters[0]);

                //context.AssertCnstr(c);
                solver.Assert(c);

                //_context.LogWriteLine(context.ToString(c));
                _context.LogWriteLine(c.ToString());
            }
        }

        /// <summary>
        /// Main visitor method to translate the LINQ expression tree into a Z3 expression handle.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        /// <param name="environment">Environment with bindings of theorem variables to Z3 handles.</param>
        /// <param name="expression">LINQ expression tree node to be translated.</param>
        /// <param name="param">Parameter used to express the constraint on.</param>
        /// <returns>Z3 expression handle.</returns>
        private Expr Visit(Context context, Environment environment, Expression expression, ParameterExpression param)
        {
            //
            // Largely table-driven mechanism, providing constructor lambdas to generic Visit*
            // methods, classified by type and arity.
            //
            switch (expression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkAnd((BoolExpr)a, (BoolExpr)b));

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkOr((BoolExpr)a, (BoolExpr)b));

                case ExpressionType.ExclusiveOr:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkXor((BoolExpr)a, (BoolExpr)b));

                case ExpressionType.Not:
                    return VisitUnary(context, environment, (UnaryExpression)expression, param, (ctx, a) => ctx.MkNot((BoolExpr)a));

                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return VisitUnary(context, environment, (UnaryExpression)expression, param, (ctx, a) => ctx.MkUnaryMinus((ArithExpr)a));

                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkAdd((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkSub((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkMul((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.Divide:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkDiv((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.Modulo:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkRem((IntExpr)a, (IntExpr)b));

                case ExpressionType.LessThan:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkLt((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.LessThanOrEqual:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkLe((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.GreaterThan:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkGt((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.GreaterThanOrEqual:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkGe((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.Equal:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkEq(a, b));

                case ExpressionType.NotEqual:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkNot(ctx.MkEq(a, b)));

                case ExpressionType.MemberAccess:
                    return VisitMember(environment, (MemberExpression)expression, param);

                case ExpressionType.Constant:
                    return VisitConstant(context, (ConstantExpression)expression);

                case ExpressionType.Call:
                    return VisitCall(context, environment, (MethodCallExpression)expression, param);

                case ExpressionType.Parameter:
                    return VisitParameter(context, environment, (ParameterExpression)expression, param);

                case ExpressionType.Convert:
                    return VisitConvert(context, environment, (UnaryExpression)expression, param);

                case ExpressionType.Power:
                    return VisitBinary(context, environment, (BinaryExpression)expression, param, (ctx, a, b) => ctx.MkPower((ArithExpr)a, (ArithExpr)b));

                case ExpressionType.Index:
                    return VisitIndex(context, environment, (IndexExpression)expression, param);

                default:
                    throw new NotSupportedException("Unsupported expression node type encountered: " + expression.NodeType);
            }
        }

        /// <summary>
        /// Visitor method to translate a binary expression.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        /// <param name="environment">Environment with bindings of theorem variables to Z3 handles.</param>
        /// <param name="expression">Binary expression.</param>
        /// <param name="ctor">Constructor to combine recursive visitor results.</param>
        /// <param name="param">Parameter used to express the constraint on.</param>
        /// <returns>Z3 expression handle.</returns>
        private Expr VisitBinary(Context context, Environment environment, BinaryExpression expression, ParameterExpression param, Func<Context, Expr, Expr, Expr> ctor)
        {
            return ctor(context, Visit(context, environment, expression.Left, param), Visit(context, environment, expression.Right, param));
        }

        /// <summary>
        /// Visitor method to translate a method call expression.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        /// <param name="environment">Environment with bindings of theorem variables to Z3 handles.</param>
        /// <param name="call">Method call expression.</param>
        /// <param name="param">Parameter used to express the constraint on.</param>
        /// <returns>Z3 expression handle.</returns>
        private Expr VisitCall(Context context, Environment environment, MethodCallExpression call, ParameterExpression param)
        {
            var method = call.Method;

            //
            // Does the method have a rewriter attribute applied?
            //
            var rewriterAttr = (TheoremPredicateRewriterAttribute)method.GetCustomAttributes(typeof(TheoremPredicateRewriterAttribute), false).SingleOrDefault();
            if (rewriterAttr != null)
            {
                //
                // Make sure the specified rewriter type implements the ITheoremPredicateRewriter.
                //
                var rewriterType = rewriterAttr.RewriterType;
                if (!typeof(ITheoremPredicateRewriter).IsAssignableFrom(rewriterType))
                    throw new InvalidOperationException("Invalid predicate rewriter type definition. Did you implement ITheoremPredicateRewriter?");

                //
                // Assume a parameterless public constructor to new up the rewriter.
                //
                var rewriter = (ITheoremPredicateRewriter)Activator.CreateInstance(rewriterType);

                //
                // Make sure we don't get stuck when the rewriter just returned its input. Valid
                // rewriters should satisfy progress guarantees.
                //
                var result = rewriter.Rewrite(call);
                if (result == call)
                    throw new InvalidOperationException("The expression tree rewriter of type " + rewriterType.Name + " did not perform any rewrite. Aborting compilation to avoid infinite looping.");

                //
                // Visit the rewritten expression.
                //
                return Visit(context, environment, result, param);
            }

            //
            // Filter for known Z3 operators.
            //
            if (method.IsGenericMethod && method.GetGenericMethodDefinition() == typeof(Z3Methods).GetMethod("Distinct"))
            {
                //
                // We know the signature of the Distinct method call. Its argument is a params
                // array, hence we expect a NewArrayExpression.
                //
                var arr = (NewArrayExpression)call.Arguments[0];
                var args = from arg in arr.Expressions select Visit(context, environment, arg, param);
                return context.MkDistinct(args.ToArray());
            }
            
            if (method.DeclaringType == typeof(Math) && method.Name == "Sqrt")
            {
                var result = Expression.Power(call.Arguments[0], Expression.Constant(2.0));
                return Visit(context, environment, result, param);
            }

            throw new NotSupportedException("Unknown method call:" + method.ToString());
        }

        /// <summary>
        /// Visitor method to translate a unary expression.
        /// </summary>
        /// <param name="context">Z3 context.</param>
        /// <param name="environment">Environment with bindings of theorem variables to Z3 handles.</param>
        /// <param name="expression">Unary expression.</param>
        /// <param name="ctor">Constructor to combine recursive visitor results.</param>
        /// <param name="param">Parameter used to express the constraint on.</param>
        /// <returns>Z3 expression handle.</returns>
        private Expr VisitUnary(Context context, Environment environment, UnaryExpression expression, ParameterExpression param, Func<Context, Expr, Expr> ctor)
        {
            return ctor(context, Visit(context, environment, expression.Operand, param));
        }

        private Expr VisitParameter(Context context, Environment environment, ParameterExpression expression, ParameterExpression param)
        {
            Expr value;
            if (!environment.TryGetValue(expression.Name, out value))
            {
                throw new NotSupportedException("Unknown parameter encountered: " + expression.Name + ".");
            }

            return value;
        }

        private Expr VisitConvert(Context context, Environment environment, UnaryExpression expression, ParameterExpression param)
        {
            if (expression.Type == expression.Operand.Type)
            {
                return Visit(context, environment, expression.Operand, param);                
            }

            var inner = Visit(context, environment, expression.Operand, param);

            switch (Type.GetTypeCode(expression.Operand.Type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                    break;
            }

            switch (Type.GetTypeCode(expression.Type))
            {
                case TypeCode.Double:
                    return context.MkInt2Real((IntExpr)inner);
                case TypeCode.Int32:
                    return context.MkReal2Int((RealExpr)inner);
                case TypeCode.Char:
                    if (inner.IsInt)
                    {
                        return inner;// context.MkInt(1);// ((IntExpr)inner).int);
                    }
                    break;
            }
            
            throw new NotImplementedException($"Cast '{expression.Operand} ({expression.Operand.Type})' to {expression.Type}");
        }

        private Expr VisitIndex(Context context, Environment environment, IndexExpression expression, ParameterExpression param)
        {
            return context.MkSelect(
                (ArrayExpr)Visit(context, environment, expression.Object, param),
                expression.Arguments.Select(a => Visit(context, environment, a, param)).ToArray());
            //, (ctx, a, b) => ctx.MkPower((ArithExpr)a, (ArithExpr)b));
            //return ctor(context, , Visit(context, environment, expression.Right, param));
        }
    }
}