// Runs the edit-mode tests without Unity: finds [Test] methods by reflection and calls them,
// with [SetUp] before each. Supports what the tests use; no [TestCase] or parameters.
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

static class Runner
{
    static int Main(string[] args)
    {
        Assembly tests = Assembly.LoadFrom(args[0]);
        int passed = 0, failed = 0;
        foreach (Type type in tests.GetTypes().Where(t => t.IsClass && !t.IsAbstract).OrderBy(t => t.FullName))
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setUp = methods.FirstOrDefault(m => m.IsDefined(typeof(SetUpAttribute), true));
            foreach (MethodInfo test in methods.Where(m => m.IsDefined(typeof(TestAttribute), true)))
            {
                try
                {
                    object fixture = Activator.CreateInstance(type);
                    if (setUp != null) setUp.Invoke(fixture, null);
                    test.Invoke(fixture, null);
                    passed++;
                }
                catch (TargetInvocationException e)
                {
                    failed++;
                    Console.WriteLine("FAIL " + type.Name + "." + test.Name + ": " + e.InnerException.Message);
                }
            }
        }
        Console.WriteLine(passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
