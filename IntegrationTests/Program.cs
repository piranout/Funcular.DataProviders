using System.Configuration;
using Funcular.DataProviders.EntityFramework;

namespace Funcular.DataProviders.IntegrationTests
{
    class Program
    {

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
        }

        internal static void TestCreate()
        {
            
        }

        internal static void TestGet()
        {
            
        }

        internal static void TestUpdate()
        {
            
        }

        internal static void TestDelete()
        {
            
        }

        internal static EntityFrameworkProvider GetEntityFrameworkProvider()
        {
            return new EntityFrameworkProvider(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }
    }
}
