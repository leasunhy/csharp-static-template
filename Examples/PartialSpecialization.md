# Partial Specialization Exmaple


## Input


```csharp
using System;
using System.Linq;

namespace Test {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine(Awesome<double, int>.add(1.0, 2.0));  // primary template
            Console.WriteLine(Awesome<int, int>.add(1, 2));         // use partial specialization
            Console.WriteLine(Awesome<int, double>.addTwice(1, 2)); // use partial specialization
        }
    }

    [StaticTemplate]
    public class Awesome<T, F> {
        public static T add(T lhs, T rhs) {
            T a;
            return lhs + rhs;
        }
        // note that the template here has no addTwice member
    }

    [StaticTemplate]
    public class Awesome<T, F> where T : IsType<int> {
        // Partial specialization Awesome<int, F>
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
            Console.WriteLine(Awesome#double, int#.add(1.0, 2.0));  // primary template
            Console.WriteLine(Awesome#int, int#.add(1, 2));         // use partial specialization
            Console.WriteLine(Awesome#int, double#.addTwice(1, 2)); // use partial specialization
        }
    }
    public class Awesome#double, int# {
        public static double add(double lhs, double rhs) {
            double a;
            return lhs + rhs;
        }
        // note that the template here has no addTwice member
    }
    public class Awesome#int, int#  {
        // Partial specialization Awesome<int, F>
        public static int add(int lhs, int rhs) {
            int a;
            return lhs + rhs;
        }

        public static int addTwice(int lhs, int rhs) {
            return lhs + rhs + rhs;
        }
    }
    public class Awesome#int, double#  {
        // Partial specialization Awesome<int, F>
        public static int add(int lhs, int rhs) {
            int a;
            return lhs + rhs;
        }

        public static int addTwice(int lhs, int rhs) {
            return lhs + rhs + rhs;
        }
    }
}
```


