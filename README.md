# Funcular.DataProviders
Self-configuring, dependency-injection-friendly, code-first Entity Framework-based data provider (repository utility). 

* Provides easy __bulk__ insert, delete and update methods without round trips to the database (New in 0.8.5)
* Automatically scans assemblies in your AppDomain for configurable entities 
* No EDMXs 
* Sets up a SQL Server context with no configuration but a connection string
* Does __not__ require you to manually add entity types to your DbSet
* By default, picks up any classes derived from EntityTypeConfiguration 
* One line of code�a predicate�specifies which Types from your assembly are entities

Currently supports SQL Server; eventually will handle MongoDB and others as well. A **[NuGet Package](https://www.nuget.org/packages/Funcular.DataProviders)** is also available.

### Usage 
You can find a complete, working usage example in the IntegrationTests project. It uses the PrototypeDatabase (included); by default, it expects it to be on the default SQL Server instance on your local machine, using Windows authentication.

A minimalistic example could be this simple. It is assumed that you either follow Entity Framework naming conventions for entities, tables, PKs, Ids, etc., or else you have created EntityTypeConfiguration classes for your entities. EntityTypeConfigurations for a type will always take precedence over the automatic mappings that EF would otherwise do.

```csharp
    // Usings:
    using Funcular.DataProviders.EntityFramework;
    // Your entity namespace:
    using Funcular.DataProviders.IntegrationTests.Entities;

    string connectionString = 
        ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
     
    this._provider = new EntityFrameworkProvider(connectionString)
    {
        // `IsEntityType` is a Func<Type,bool> that tells the provider
        // how to recognize which classes are entities. This could refer
        // to a base type, a namespace, or any common attribute of your
        // entity types:
        IsEntityType = type => type.IsSubclassOf(typeof(Createable<>))
    };

    // .Insert is kind of self explanatory:
    DescribedThing described = CreateEntityInstance(); // your sample data function here
    this._provider.Insert<DescribedThing, string>(described);

    // Assume you have an entity called "DescribedThing"...
    // Use .Get to fetch by Id when you know an item exists:
    var retrieved = this._provider.Get<DescribedThing, string>(id);
    
    // Use .Query when you aren't certain:
    var retrieved = this._provider.Query<DescribedThing>()
        .FirstOrDefault(myDescribed => myDescribed.Id == id);

    // .Insert and .Update have overloads for working with single instances or collections.
```

Optional: If you derive your entities from Funcular.Ontology interfaces and base classes, it makes it easier to apply common behavior to the entire domain.
* You can apply a common Id-assignment method to all entities
* The data provider will automatically handle audit fields like creation / modification dates and users. 
```csharp
    // The example uses Base36 CHAR(20) Ids; you don't have to:
    var idGenerator = new Base36IdGenerator(
                numTimestampCharacters: 11,
                numServerCharacters: 4,
                numRandomCharacters: 5,
                reservedValue: "",
                delimiter: "-",
                delimiterPositions: new[] { 15, 10, 5 });

    // This uses Funcular.Ontology's `Creatable` base class to reference a common 
    // Id-generation method for all entities(); you can handle this however you like:
    Createable<string>.IdentityFunction = () => idGenerator.NewId();

    // Some audit fields that Ontology base classes provide and manage for you:
    var entityInstance = GetEntityInstance(); // your logic
    var inserted = this._provider.Insert<DescribedThing, string>(described);
    Assert.IsTrue(inserted.CreatedBy.HasValue());
    Assert.IsTrue(inserted.DateCreatedUtc != default(DateTime));

    inserted.SomeProperty = "SomeDifferentValue";
    var updated = this._provider.Update<DescribedThing, string>(inserted);
    Assert.IsNotNull(updated.DateModifiedUtc);
    Assert.IsNotNull(updated.ModifiedBy);
```

