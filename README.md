# C++-like Templates for C#

This repository implements a Roslyn-based compiler for C#,
which supports C++-like static templates.

Currently a *POC*, the compiler only supports compilation of a single file or a single solution,
and does not accept any command-line arguments.

See section [Roadmap](#roadmap) to check which features are implemented.

## Usage

### Compiling single C# file

```cmd
StaticTemplate.exe test.cs
```

... and a `test.exe` will be generated in current directory.

*Note*: since it's hard to infer the references from the `using` directives,
compiling single C# file supports limited references (currently fixed),
and is only for debug use.
Please use the method in [Compiling a solution](compiling-a-solution) instead
if you need custom references.

### Compiling a solution

```cmd
StaticTemplate.exe D:\AwesomeSolution\AwesomeSolution.sln
```

Then the compiler will compile the projects in solution `AwesomeSolution.sln`,
in topological order.

Assembly files of all projects will be written to current working directory (to be tuned).

## Example

A basic example input file:

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

Though not shown in the example,
normal generic classes will not be affected.

For more examples, look into the `Examples` folder.

## Roadmap

* [x] Template instantiation via type parameter substitution.
* [x] Full specialization.
* [x] Partial specialization.
* [x] Cross-file template instantiation.
* [ ] Cross-assembly template instantiation.
* [ ] Function template.
* [ ] Support other types (`int`, `bool`, etc.) of template parameters.
* [ ] Better runtime support (proper way to do reflection, etc.).
* [ ] Better syntax.
* [ ] Type aliasing.
* [ ] Default type arguments.
* [ ] Other type constraints besides `IsType<T>` for specialization.
* [ ] SFINAE.

## License

MIT.
