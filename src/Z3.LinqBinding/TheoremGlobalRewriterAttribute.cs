using System;

namespace Z3.LinqBinding
{
    public class TheoremGlobalRewriterAttribute : Attribute
    {
        public Type RewriterType { get; set; }
    }
}