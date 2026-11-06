// Runs the edit-mode tests without Unity: finds [Test] and [TestCase] methods by reflection and
// calls them on a fresh fixture, with [SetUp] before each. A [Test] method with parameters but
// no [TestCase] counts as a failure.
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

static class Runner
{
    static int passed, failed;

    static void Run(Type type, MethodInfo setUp, MethodInfo test, object[] arguments)
    {
        string name = type.Name + "." + test.Name + (arguments == null ? "" : "(" + string.Join(", ", arguments.Select(a => Convert.ToString(a)).ToArray()) + ")");
        try
        {
            object fixture = Activator.CreateInstance(type);
            if (setUp != null) setUp.Invoke(fixture, null);
            test.Invoke(fixture, arguments);
            passed++;
        }
        catch (TargetInvocationException e)
        {
            failed++;
            Console.WriteLine("FAIL " + name + ": " + e.InnerException.Message);
        }
    }

    static int Main(string[] args)
    {
        Assembly tests = Assembly.LoadFrom(args[0]);
        foreach (Type type in tests.GetTypes().Where(t => t.IsClass && !t.IsAbstract).OrderBy(t => t.FullName))
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodInfo setUp = methods.FirstOrDefault(m => m.IsDefined(typeof(SetUpAttribute), true));
            foreach (MethodInfo test in methods)
            {
                object[] cases = test.GetCustomAttributes(typeof(TestCaseAttribute), true);
                bool isTest = test.IsDefined(typeof(TestAttribute), true);
                if (isTest && test.GetParameters().Length == 0) Run(type, setUp, test, null);
                foreach (object a in cases) Run(type, setUp, test, ((TestCaseAttribute)a).Arguments);
                if (isTest && test.GetParameters().Length > 0 && cases.Length == 0)
                {
                    failed++;
                    Console.WriteLine("FAIL " + type.Name + "." + test.Name + ": has parameters but no [TestCase]");
                }
            }
        }
        Console.WriteLine(passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
