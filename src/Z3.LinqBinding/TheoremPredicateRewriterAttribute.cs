using System;

namespace Z3.LinqBinding
{
    public class TheoremPredicateRewriterAttribute : Attribute
    {
        public Type RewriterType { get; set; }
    }
}