#region File info
// *********************************************************************************************************
// Funcular.DataProviders>Funcular.DataProviders>BaseContext.cs
// Created: 2015-07-01 3:53 PM
// Updated: 2015-07-01 3:56 PM
// By: Paul Smith 
// 
// *********************************************************************************************************
// LICENSE: The MIT License (MIT)
// *********************************************************************************************************
// Copyright (c) 2010-2015 <copyright holders>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// *********************************************************************************************************
#endregion


#region Usings
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
#endregion


namespace Funcular.DataProviders.EntityFramework.SqlServer
{
    [DbConfigurationType(typeof (SqlServerInvariantDbConfiguration))]
    public class BaseContext : DbContext
    {
        private static readonly HashSet<string> _entityAssemblyNames = new HashSet<string>();

        private static readonly Type _entityTypeConfigurationType =
            typeof (EntityTypeConfiguration<>);

        private static readonly Type _complexTypeConfigurationType =
            typeof (ComplexTypeConfiguration<>);

        private static readonly List<Type> _typesToIgnore = new List<Type>();

        private static Func<Type, bool> _isEntityType = type => type.IsClass && !type.IsAbstract;

        private static readonly object _lock = new object();

        protected readonly HashSet<string> _configuredAssemblies = new HashSet<string>();


        public BaseContext()
        {
        }

        /// <summary>
        ///     <paramref name="nameOrConnectionString">
        ///         A connection string, or the name of a ConnectionStrings config file entry.
        ///     </paramref>
        ///     >
        /// </summary>
        /// <param name="nameOrConnectionString"></param>
        public BaseContext(String nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            //Stop code first migrations from happening automatically:
            Database.SetInitializer<BaseContext>(null);
        }

        public static List<Type> TypesToIgnore => _typesToIgnore;

        public Func<Type, bool> IsEntityType { get { return _isEntityType; } set { _isEntityType = value; } }

        public static HashSet<string> EntityAssemblyNames => _entityAssemblyNames;

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            lock (_lock)
            {
                modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

                configureModel(modelBuilder);

                //Specify a configuration if you would like to use code first migration, e.g.:
                //Database.SetInitializer(new MigrateDatabaseToLatestVersion<Context, Configuration>());
                Database.SetInitializer<BaseContext>(null);

                base.OnModelCreating(modelBuilder);
            }
        }

        /// <summary>
        ///     Configure the model.
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected void configureModel(DbModelBuilder modelBuilder)
        {
            //Get the Entity method of the DbModelBuilder. The Entity method is used
            //to register an EntityType with the model builder.
            var entityMethod = typeof (DbModelBuilder).GetMethod("Entity");

            //Get objects of type BaseEntity referenced in any of the assemblies. That will ensure that
            //they can be used by any contexts which extend this context.
            //First build a set of all of the Assembly names which should be examined
            //through reflection for any objects extending BaseEntity
            // TODO: Make a set for Mapping classes; expose publicly
            IEqualityComparer<AssemblyName> equalityComparer =
                new FuncEqualityComparer<AssemblyName>((name, assemblyName) => name.FullName == assemblyName.FullName);
            var assemblyNames = new HashSet<AssemblyName>(equalityComparer);
            
            IsEntityType = IsEntityType ?? (type => type.Namespace != null && type.Namespace.EndsWith(".Entities"));

            IList<Assembly> assys =
                GetAppDomainAssemblies()
                    .Where(assembly => !assemblyIsMicrosoft(assembly))
                    .Where(assemblyContainsEntitiesOrMappings)
                    .ToList();
            foreach (var assembly in assys)
            {
                AddAssemblyNameToSet(assembly.GetName(), assemblyNames);
            }
            //For each assembly we find the classes in it of type EntityBase and 
            //register them with the modelBuilder
            foreach (var name in assemblyNames)
            {
                registerEntityTypesFromAssembly(name, modelBuilder, entityMethod);
            }
        }

        private static IEnumerable<Assembly> GetAppDomainAssemblies()
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return Enumerable.Empty<Assembly>().ToArray();
            }
        }

        /// <summary>
        ///     Returns true if exception encountered during introspection
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static bool assemblyIsMicrosoft(Assembly assembly)
        {
            try
            {
                return assembly.FullName.StartsWith("mscorlib")
                       || assembly.FullName.StartsWith("Microsoft")
                       || assembly.FullName.StartsWith("System")
                       || assembly.FullName.StartsWith("EntityFramework");
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        ///     Returns false  if exception encountered during introspection
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static bool assemblyContainsEntitiesOrMappings(Assembly assembly)
        {
            try
            {
                var types = getTypes(assembly).ToArray();
                return types.Any(type =>
                    _isEntityType(type))
                       || types.Any(isMappingType);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static IEnumerable<Type> getTypes(Assembly a)
        {
            try
            {
                return a.GetTypes();
            }
            catch (Exception)
            {
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        ///     Add an AssemblyName to the set of AssemblyNames. This will first search the
        ///     names already in the set and not add it if an AssemblyName with the same FullName
        ///     property exists.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assemblyNames"></param>
        private void AddAssemblyNameToSet(AssemblyName name, HashSet<AssemblyName> assemblyNames)
        {
            assemblyNames.Add(name);
        }

        /// <summary>
        ///     For an assembly, find all of it's classes which are of type BaseEntity and then invoke the
        ///     Entity method on the ModelBuilder with the type of the class found which extends BaseEntity.
        ///     This then registers that class in the context model builder.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="modelBuilder"></param>
        /// <param name="entityMethod"></param>
        protected void registerEntityTypesFromAssembly(AssemblyName a, DbModelBuilder modelBuilder,
            MethodInfo entityMethod)
        {
            if (!this._configuredAssemblies.Add(a.Name))
                return;
            try
            {
                var configurations = buildConfigurations(a);
                var configEntityTypes = configurations.Keys.Select(
                    k => k.GetGenericArguments()
                        .First()
                        .Name);

                foreach (var config in configurations)
                {
                    config.Key.Invoke(
                        modelBuilder.Configurations,
                        new[] {config.Value});
                }
                // leave entities alone if they've already been configured with code-first mappings:
                var entityTypes = Assembly.Load(a)
                    .GetTypes()
                    .Where(
                        x => IsEntityType(x)
                             && !configEntityTypes.Contains(x.Name)
                             && !TypesToIgnore.Contains(x));
                foreach (var entType in entityTypes.Distinct())
                {
                    entityMethod.MakeGenericMethod(entType)
                        .Invoke(modelBuilder, new object[] {});
                }
            }
            catch (Exception e)
            {
                //Bury exception if the assembly cannot be processed. No need to raise any further.
                //Log as a debug below, not an error, so we can see what assemblies cannot be loaded
                //when in debug mode.
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        ///     Gets a dictionary of add methods for configurations exposed in the given assembly
        /// </summary>
        /// <param name="a">Name of assembly that's being probed.</param>
        /// <returns>List of add methods</returns>
        private static IDictionary<MethodInfo, object> buildConfigurations(AssemblyName a)
        {
            //Get all add methods from the ConfigurationRegistrar class
            var addMethods = typeof (ConfigurationRegistrar).GetMethods()
                .Where(m => m.Name.Equals("Add"))
                .ToList();

            //Get add method where the configuration is assignable from EntityTypeConfiguration
            var entityTypeMethod = addMethods.First(
                m =>
                    m.GetParameters()
                        .First()
                        .ParameterType
                        .GetGenericTypeDefinition()
                        .IsAssignableFrom(_entityTypeConfigurationType));

            //Get add method where the configuration is assignable from ComplexTypeConfiguration
            var complexTypeMethod = addMethods.First(
                m =>
                    m.GetParameters()
                        .First()
                        .ParameterType
                        .GetGenericTypeDefinition()
                        .IsAssignableFrom(_complexTypeConfigurationType));

            var configurations = new Dictionary<MethodInfo, object>();

            //Get all entities
            var types = Assembly.Load(a)
                .GetExportedTypes()
                .Where(isMappingType)
                .ToList();

            foreach (var type in types)
            {
                MethodInfo typedMethod;
                Type modelType;

                if (isMatching(
                    type,
                    out modelType,
                    t => _entityTypeConfigurationType.IsAssignableFrom(t)))
                {
                    typedMethod = entityTypeMethod.MakeGenericMethod(
                        modelType);
                }
                else if (isMatching(
                    type,
                    out modelType,
                    t => _complexTypeConfigurationType.IsAssignableFrom(t)))
                {
                    typedMethod = complexTypeMethod.MakeGenericMethod(
                        modelType);
                }
                else
                    continue;

                //Add configuration method
                if (!configurations.ContainsKey(typedMethod))
                {
                    configurations.Add(
                        typedMethod,
                        Activator.CreateInstance(type));
                }
            }

            return configurations;
        }

        /// <summary>
        ///     Finds the configuration class that matches a given type.
        /// </summary>
        /// <param name="matchingType"></param>
        /// <param name="modelType"></param>
        /// <param name="matcher"></param>
        /// <returns></returns>
        private static bool isMatching(
            Type matchingType,
            out Type modelType,
            Predicate<Type> matcher)
        {
            modelType = null;

            while (matchingType != null)
            {
                if (matchingType.IsGenericType)
                {
                    var definitionType = matchingType
                        .GetGenericTypeDefinition();

                    if (matcher(definitionType))
                    {
                        modelType = matchingType.GetGenericArguments()
                            .First();
                        return true;
                    }
                }

                matchingType = matchingType.BaseType;
            }
            return false;
        }

        /// <summary>
        ///     Tests if given type is a configuration class
        /// </summary>
        /// <param name="matchingType">Type that is being tested</param>
        /// <returns>true if type is a configuration class</returns>
        private static bool isMappingType(Type matchingType)
        {
            if (!matchingType.IsClass ||
                matchingType.IsAbstract)
                return false;
            Type temp;

            return isMatching(
                matchingType,
                out temp,
                t =>
                    _entityTypeConfigurationType.IsAssignableFrom(t) ||
                    _complexTypeConfigurationType.IsAssignableFrom(t));
        }
    }

    public class SqlServerDbContextConfiguration : DbConfiguration
    {
        public SqlServerDbContextConfiguration()
        {
            SetProviderServices(SqlProviderServices.ProviderInvariantName, SqlProviderServices.Instance);
        }
    }

    internal class FuncEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparer;
        private readonly Func<T, int> _hash;

        public FuncEqualityComparer(Func<T, T, bool> comparer)
            : this(comparer, t => 0)
            // NB Cannot assume anything about how e.g., t.GetHashCode() interacts with the comparer's behavior
        {
        }

        public FuncEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hash)
        {
            this._comparer = comparer;
            this._hash = hash;
        }


        #region IEqualityComparer<T> Members
        public bool Equals(T x, T y)
        {
            return this._comparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return this._hash(obj);
        }
        #endregion
    }
}