# Full Specialization Exmaple


## Input


```csharp
using System;
using System.Linq;

namespace Test {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine(Adder<int>.add(1, 2));
            Console.WriteLine(Adder<double>.add(1.0, 2.0));

            Console.WriteLine(Adder<int>.addTwice(1, 2));
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
        // note that the template here has no addTwice member
    }

    [StaticTemplate]
    public class Adder<T> where T : IsType<int> {
        // Full specialization of Adder<int>
        public static T add(T lhs, T rhs) {
            T a;
            return lhs + rhs;
        }

        public static T addTwice(T lhs, T rhs) {
            return lhs + rhs + rhs;
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

            Console.WriteLine(Adder#int#.addTwice(1, 2));
        }
    }
    public class Adder#int#  {
        // Full specialization of Adder<int>
        public static int add(int lhs, int rhs) {
            int a;
            return lhs + rhs;
        }

        public static int addTwice(int lhs, int rhs) {
            return lhs + rhs + rhs;
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
        // note that the template here has no addTwice member
    }
}
```

