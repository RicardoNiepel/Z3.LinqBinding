using System;

namespace Z3.LinqBinding
{
    /// <summary>
    /// https://rosettacode.org/wiki/Convert_decimal_number_to_rational#C.23
    /// </summary>
    public class Fraction
    {
        public long Numerator { get; }
        public long Denominator { get; }

        public Fraction(double f, long MaximumDenominator = 4096)
        {
            /* Translated from the C version. */
            /*  a: continued fraction coefficients. */
            long a;
            var h = new long[3] { 0, 1, 0 };
            var k = new long[3] { 1, 0, 0 };
            long x, d, n = 1;
            int i, neg = 0;

            if (MaximumDenominator <= 1)
            {
                Denominator = 1;
                Numerator = (long)f;
                return;
            }

            if (f < 0) { neg = 1; f = -f; }

            while (f != Math.Floor(f)) { n <<= 1; f *= 2; }
            d = (long)f;

            /* continued fraction and check denominator each step */
            for (i = 0; i < 64; i++)
            {
                a = (n != 0) ? d / n : 0;
                if ((i != 0) && (a == 0)) break;

                x = d; d = n; n = x % n;

                x = a;
                if (k[1] * a + k[0] >= MaximumDenominator)
                {
                    x = (MaximumDenominator - k[0]) / k[1];
                    if (x * 2 >= a || k[1] >= MaximumDenominator)
                        i = 65;
                    else
                        break;
                }

                h[2] = x * h[1] + h[0]; h[0] = h[1]; h[1] = h[2];
                k[2] = x * k[1] + k[0]; k[0] = k[1]; k[1] = k[2];
            }
            Denominator = k[1];
            Numerator = neg != 0 ? -h[1] : h[1];
        }

        public override string ToString()
        {
            return string.Format("{0} / {1}", Numerator, Denominator);
        }
    }
}
