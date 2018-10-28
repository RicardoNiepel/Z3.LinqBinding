using System;
using System.Collections.Generic;
using Z3.LinqBinding;

namespace Z3.LinqBinding
{
    public class ShortestTheoremSearcher<T>
    {

        private ShortestTheoremSearcher()
        {
            _IsMinimal = true;
        }

        private ShortestTheoremSearcher(int maxLength, Func<Z3Context, int, Theorem<T>> theoremBuilder, Func<T, int> lengthBuilder)
        {
            MaxLength = maxLength;
            _TheoremBuilder = theoremBuilder;
            _LengthBuilder = lengthBuilder;

        }

        private Func<Z3Context, int, Theorem<T>> _TheoremBuilder;
        private Func<T, int> _LengthBuilder;

        private static Dictionary<int, T> _solutions = new Dictionary<int, T>();


        public int MaxLength { get; set; }

        private bool? _IsMinimal;


        public int Length(Z3Context ctx)
        {

            var sol = GetSolution(ctx, MaxLength);
            if (sol != null)
            {
                return _LengthBuilder.Invoke(sol);
            }
            return -1;
        }

        public T Solution(Z3Context ctx)
        {
            return GetSolution(ctx, MaxLength);
        }


        public T GetSolution(Z3Context ctx, int maxLength)
        {
            T toReturn;
            if (!_solutions.TryGetValue(maxLength, out toReturn))
            {

                var theorem = this._TheoremBuilder.Invoke(ctx, maxLength);
                toReturn = theorem.Solve();
                _solutions[maxLength] = toReturn;
            }


            return toReturn;
        }



        public bool IsMinimal(Z3Context ctx)
        {

            if (!_IsMinimal.HasValue)
            {
                if (Length(ctx) == -1)
                    _IsMinimal = false;
                else
                {
                    var sol = GetSolution(ctx, MaxLength - 1);
                    _IsMinimal = sol == null;
                }
            }
            return _IsMinimal.Value;
        }

        public static T SearchMinimal(int maxLength, Func<Z3Context, int, Theorem<T>> theoremBuilder,
          Func<T, int> lengthBuilder)
        {

            var listToSearch = BuildBinarySearchList(maxLength, theoremBuilder, lengthBuilder);
            var target = new ShortestTheoremSearcher<T>();
            int idxFound = -1;
            using (var ctx = new Z3Context())
            {
                idxFound = listToSearch.BinarySearch(target,
                    (obj1, obj2) => (obj1.IsMinimal(ctx) ? 0 : obj1.Length(ctx)) - (obj2.IsMinimal(ctx) ? 0 : obj2.Length(ctx)));
                if (idxFound > 0)
                {
                    return listToSearch[idxFound].Solution(ctx);
                }
            }

            return default(T);
        }


        public static List<ShortestTheoremSearcher<T>> BuildBinarySearchList(int maxLength, Func<Z3Context, int, Theorem<T>> theoremBuilder, Func<T, int> lengthBuilder)
        {
            var toReturn = new List<ShortestTheoremSearcher<T>>(maxLength);
            for (int i = 0; i < maxLength; i++)
            {
                toReturn.Add(new ShortestTheoremSearcher<T>(i, theoremBuilder, lengthBuilder));
            }
            return toReturn;
        }



    }
}