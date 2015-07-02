using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Funcular.DataProviders.EntityFramework;
using Funcular.DataProviders.IntegrationTests.Entities;
using Funcular.IdGenerators.Base36;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.IntegrationTests
{
    class Program
    {
        private static EntityFrameworkProvider _provider;
        private static readonly Random _rnd = new Random();

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            Bootstrap();
            TestCreate();
        }

        /// <summary>
        /// This all would normally be done in your IoC container
        /// </summary>
        private static void Bootstrap()
        {
            var base36 = new Base36IdGenerator(
                numTimestampCharacters: 12, 
                numServerCharacters: 6, 
                numRandomCharacters: 7, 
                reservedValue: "", 
                delimiter: "-", 
                delimiterPositions: new[] { 20, 15, 10, 5 });
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            _provider = new EntityFrameworkProvider(connectionString)
            {
                IsEntityType = type => type.IsSubclassOf(typeof(Createable<>))
            };
            _provider.SetCurrentUser("Funcular\\Paul");
            Createable<string>.IdentityFunction = () => base36.NewId();
        }

        internal static void TestCreate()
        {
            var texts = new[] {"Quae sit airspeed velocitati exoneratis hirundo?", "African vel Europae?"}.ToList();
            for (int i = 0; i < 100; i++)
            {
                var described = new MyDescribed()
                {
                    Description = string.Format("{0} {1}", MockData.Product.Department(), DateTime.Now.TimeOfDay.ToString()),
                    Label = MockData.Internet.DomainWord() + " " + DateTime.Now.Ticks.ToString(),
                    MyNullableIntProperty = _rnd.Next(10) < 6 ? null : (int?)_rnd.Next(1000000),
                    MyBoolProperty = _rnd.Next(2) == 1,
                    MyTextProperty = texts[i % 2], //"Quae sit airspeed velocitati exoneratis hirundo?",
                    Name = MockData.Person.FirstName()
                };
                _provider.Insert<MyDescribed, string>(described);
                //Thread.Sleep(10);   
            }
/*            described = new MyDescribed()
            {
                Description = MockData.Product.Department() + DateTime.Now.TimeOfDay.ToString(),
                Label = "Call me " + DateTime.Now.Ticks.ToString(),
                MyNullableIntProperty = _rnd.Next(1) == 1 ? null : (int?)DateTime.Now.TimeOfDay.TotalSeconds,
                MyBoolProperty = false,
                MyTextProperty = "African vel Europae?",
                Name = "Caesar"
            };
            _provider.Save<MyDescribed, string>(described);*/
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
