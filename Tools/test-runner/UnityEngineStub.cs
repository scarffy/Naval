// Stands in for the one UnityEngine member the tests use, so they run on plain Mono.
namespace UnityEngine
{
    public static class Application
    {
        public static string dataPath { get { return System.Environment.GetEnvironmentVariable("NAVAL_ASSETS"); } }
    }
}
