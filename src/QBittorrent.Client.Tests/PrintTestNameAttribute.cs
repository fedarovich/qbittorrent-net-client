using System;
using System.Reflection;
using Xunit.Sdk;

namespace QBittorrent.Client.Tests
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PrintTestNameAttribute : BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest)
        {
            Console.WriteLine("\tRunning test " + methodUnderTest.Name);
            base.After(methodUnderTest);
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            base.Before(methodUnderTest);
            Console.WriteLine("\t------------------------------");
        }
    }
}
