# Z3.LinqBinding
LINQ to Z3 - an esoteric LINQ binding based on Bart De Smet's idea

## Original Idea
In 2009 Bart De Smet created a LINQ to Z3 binding and describes it in three blog posts
* [EXPLORING THE Z3 THEOREM PROVER (WITH A BIT OF LINQ)](http://community.bartdesmet.net/blogs/bart/archive/2009/04/15/exploring-the-z3-theorem-prover-with-a-bit-of-linq.aspx)
* [LINQ TO Z3 – THEOREM SOLVING ON STEROIDS – PART 0](http://community.bartdesmet.net/blogs/bart/archive/2009/04/19/linq-to-z3-theorem-solving-on-steroids-part-0.aspx)
* [LINQ TO Z3 – THEOREM SOLVING ON STEROIDS – PART 1](http://community.bartdesmet.net/blogs/bart/archive/2009/09/27/linq-to-z3-theorem-solving-on-steroids-part-1.aspx)

## This Version
Since 2009 the Z3 Theorem Prover, initial from Microsoft Research, has changed:
* new API versions with a lot of breaking changes
* in March 2015 the licence has moved to MIT License and the source code to GitHub: https://github.com/Z3Prover/z3

This version of **Z3.LinqBinding** supports Z3 4.4.0.<br/>
Also the sample code of a Sudoku theorem extension has been added: **Z3.LinqBinding.Sudoku**

## Documentation

### Basic Usage
```C#
using (var ctx = new Z3Context())
{
    ctx.Log = Console.Out; // see internal logging

    var theorem = from t in ctx.NewTheorem(new { x = default(bool), y = default(bool) })
                  where t.x ^ t.y
                  select t;

    var result = theorem.Solve();
    Console.WriteLine(result);
}
```

**Output:**
```
(xor x y)
{ x = True, y = False }
```

### Advanced Usage
You can use build in or custom symbol classes.<br/>
Currently only bool and integer theorems are supported.

```C#
using (var ctx = new Z3Context())
{
    ctx.Log = Console.Out; // see internal logging

    var theorem = from t in ctx.NewTheorem<Symbols<int, int>>()
                  where t.X1 < t.X2 + 1
                  where t.X1 > 2
                  where t.X1 != t.X2
                  select t;

    var result = theorem.Solve();
    Console.WriteLine(result);
}
```

**Output:**
```
(< X1 (+ X2 1))
(> X1 2)
(not (= X1 X2))
{X1 = 3, X2 = 4}
```

### Sudoku Extension Usage (Z3.LinqBinding.Sudoku)
```C#
using (var ctx = new Z3Context())
{
    var theorem = from t in SudokuTheorem.Create(ctx)
                  where t.Cell13 == 2 && t.Cell16 == 1 && t.Cell18 == 6
                  where t.Cell23 == 7 && t.Cell26 == 4
                  where t.Cell31 == 5 && t.Cell37 == 9
                  where t.Cell42 == 1 && t.Cell44 == 3
                  where t.Cell51 == 8 && t.Cell55 == 5 && t.Cell59 == 4
                  where t.Cell66 == 6 && t.Cell68 == 2
                  where t.Cell73 == 6 && t.Cell79 == 7
                  where t.Cell84 == 8 && t.Cell87 == 3
                  where t.Cell92 == 4 && t.Cell94 == 9 && t.Cell97 == 2
                  select t;

    var result = theorem.Solve();
    Console.WriteLine(result);
}
```

**Output:**
```
-------------------------------
| 4  3  2 | 5  9  1 | 7  6  8 |
|         |         |         |
| 9  6  7 | 2  8  4 | 1  3  5 |
|         |         |         |
| 5  8  1 | 6  7  3 | 9  4  2 |
-------------------------------
| 6  1  4 | 3  2  8 | 5  7  9 |
|         |         |         |
| 8  2  3 | 7  5  9 | 6  1  4 |
|         |         |         |
| 7  5  9 | 4  1  6 | 8  2  3 |
-------------------------------
| 2  9  6 | 1  3  5 | 4  8  7 |
|         |         |         |
| 1  7  5 | 8  4  2 | 3  9  6 |
|         |         |         |
| 3  4  8 | 9  6  7 | 2  5  1 |
-------------------------------
```

### Blog Posts
* [May 9 2015 - Z3.LinqBinding released](http://blogs.msdn.com/b/riwickel/archive/2015/05/09/z3-linqbinding-released.aspx)