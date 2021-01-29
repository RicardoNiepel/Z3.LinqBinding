using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Z3.LinqBinding
{
    public class Environment
    {
        private Dictionary<PropertyInfo, Expr> _env = new Dictionary<PropertyInfo, Expr>();

        public virtual bool TryGetExpression(MemberExpression member, ParameterExpression param, out Expr expr)
        {
            if (member.Expression != param)
            {
                throw new NotSupportedException("Encountered member access not targeting the constraint parameter.");
            }

            PropertyInfo property = member.Member as PropertyInfo;
            return _env.TryGetValue(property, out expr);
        }

        public virtual bool TryGetValue(string name, out Expr expr)
        {
            var propertyInfo = _env.FirstOrDefault(p => p.Key.Name == name).Key;
            return _env.TryGetValue(propertyInfo, out expr);
        }

        public virtual void Add(PropertyInfo parameter, Context context)
        {
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
                    Add(parameter, context.MkBoolConst(parameter.Name));
                    break;
                case TypeCode.Int32:
                    //environment.Add(parameter, context.MkConst(parameter.Name, context.MkIntType()));
                    Add(parameter, context.MkIntConst(parameter.Name));
                    break;
                case TypeCode.Double:
                    Add(parameter, context.MkRealConst(parameter.Name));
                    break;
                default:
                    throw new NotSupportedException($"Parameter type {parameterType.Name} of {parameter.Name} is not supported.");
            }
        }

        public virtual void Add(PropertyInfo propertyInfo, Expr expr) 
        {
            _env.Add(propertyInfo, expr);
        }

        public IEnumerable<PropertyInfo> Keys => _env.Keys;

        public Expr this[PropertyInfo propertyInfo]=> _env[propertyInfo];
    }
}
