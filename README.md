# C++-like Templates for C#

This repository implements a Roslyn-based compiler for C#,
which supports C++-like static templates.

Currently a *POC*, the compiler only supports compilation of a single file,
and does not accept any command-line arguments.

See section [Roadmap](#roadmap) to check which features are implemented.

## Usage

```cmd
StaticTemplate.exe test.cs
```

... and a `test.exe` will be generated in current directory.

## Example

A trivial example input file:

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

        T hhhh { get; set; }

        Func<T, T, T> adder = (l, r) => l + r;

        public static T add(T lhs, T rhs) {
            T a;
            return lhs + rhs;
        }
    }
}

```

The intermediate result (equivalent code, not valid C#,
*note the identifier name*):

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

        int hhhh { get; set; }

        Func<int, int, int> adder = (l, r) => l + r;

        public static int add(int lhs, int rhs) {
            int a;
            return lhs + rhs;
        }
    }
    public class Adder#double# {
        double T;

        double hhhh { get; set; }

        Func<double, double, double> adder = (l, r) => l + r;

        public static double add(double lhs, double rhs) {
            double a;
            return lhs + rhs;
        }
    }
}
```

Though not shown in the example,
normal generic classes will not be affected.

## Roadmap

* [x] Type parameter substitution.
* [ ] Specialization.
* [ ] Partial specialization.
* [ ] Multiple file compilation support.
* [ ] Cross-assembly compilation support.
* [ ] Function template.
* [ ] SFINAE.
* [ ] Better runtime support (proper way to do reflection, etc.).

## License

MIT.
