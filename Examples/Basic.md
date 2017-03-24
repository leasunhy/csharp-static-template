# Basic Exmaple


## Input


```csharp
using System;
using System.Linq;

namespace Test {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine(Adder<int>.add(1, 2));
            Console.WriteLine(Adder<double>.add(1.0, 2.0));
        }
    }

    [StaticTemplate]
    public class Adder<T> {
        T T;

        T TProp { get; set; }

        Func<T, T, T> adder = (l, r) => l + r;

        public static T add(T lhs, T rhs) {
            T a;
            return lhs + rhs;
        }
    }
}

```

## Output

The intermediate result (equivalent code, not valid C#: *note the identifier name*):

```csharp
using System;
using System.Linq;

namespace Test {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine(Adder#int#.add(1, 2));
            Console.WriteLine(Adder#double#.add(1.0, 2.0));
        }
    }
    public class Adder#int# {
        int T;

        int TProp { get; set; }

        Func<int, int, int> adder = (l, r) => l + r;

        public static int add(int lhs, int rhs) {
            int a;
            return lhs + rhs;
        }
    }
    public class Adder#double# {
        double T;

        double TProp { get; set; }

        Func<double, double, double> adder = (l, r) => l + r;

        public static double add(double lhs, double rhs) {
            double a;
            return lhs + rhs;
        }
    }
}
```
