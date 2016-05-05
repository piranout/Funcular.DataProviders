using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Funcular.DataProviders.EntityFramework;
using Funcular.DataProviders.IntegrationTests.Entities;
using Funcular.ExtensionMethods;
using Funcular.IdGenerators.Base36;
using Funcular.Ontology.Archetypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MockData;

namespace Funcular.DataProviders.IntegrationTests
{
    [TestClass]
    public class ConcurrentEntityFrameworkProviderIntegrationTests
    {
        private readonly object _lockObj = new object();
        private readonly Random _rnd = new Random();
        private Base36IdGenerator _base36;
        private ConcurrentEntityFrameworkProvider _provider;

        public Random Rnd
        {
            get
            {
                lock (this._lockObj)
                {
                    return this._rnd;
                }
            }
        }

        [TestInitialize]
        public void Setup()
        {
            // This is the stuff you would normally do when configuring your IOC container.
            this._base36 = new Base36IdGenerator(
                numTimestampCharacters: 11,
                numServerCharacters: 4,
                numRandomCharacters: 5,
                reservedValue: "",
                delimiter: "-",
                delimiterPositions: new[] {15, 10, 5});
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            this._provider = new ConcurrentEntityFrameworkProvider(connectionString)
            {
                IsEntityType = type => type.IsSubclassOf(typeof (Createable<>))
            };
            this._provider.SetCurrentUser("Funcular\\Paul");
            this._provider.Log = s => Debug.WriteLine(s);
            Createable<string>.IdentityFunction = () => this._base36.NewId();
        }

        [TestMethod]
        public void Created_Entity_Is_Assigned_Id()
        {
            var described = CreateDescribedThing();
            Assert.IsTrue(described.Id.HasValue() && described.Id.Length == 20);
        }

        [TestMethod]
        public void Created_Entity_Is_Assigned_DateCreatedUtc()
        {
            var described = CreateDescribedThing();
            Assert.IsTrue(described.DateCreatedUtc != default(DateTime));
        }

        [TestMethod]
        public void Created_Entity_Is_Assigned_CreatedBy()
        {
            var described = CreateDescribedThing();
            var inserted = this._provider.Insert<DescribedThing, string>(described);
            Assert.IsTrue(inserted.CreatedBy.HasValue());
        }

        [TestMethod]
        public void Created_Entity_Is_Persisted()
        {
            var described = CreateDescribedThing();
            var id = described.Id;
            try
            {
                var inserted = this._provider.Insert<DescribedThing, string>(described);
                var fetched = this._provider.Get<DescribedThing, string>(id);
                Assert.IsNotNull(fetched);
            }
            catch (Exception e)
            {
                Assert.Fail("Exception fetching entity: {0}", e);
            }
        }

        [TestMethod]
        public void Modified_Entity_Is_Assigned_ModifiedDateUtc()
        {
            var described = CreateDescribedThing();
            var id = described.Id;
            this._provider.Insert<DescribedThing, string>(described);
            var retrieved = this._provider.Get<DescribedThing, string>(id);
            retrieved.BoolProperty = !retrieved.BoolProperty;
            var updated = this._provider.Update<DescribedThing, string>(retrieved);
            Assert.IsNotNull(updated.DateModifiedUtc);
        }

        [TestMethod]
        public void Modified_Entity_Is_Assigned_ModifiedBy()
        {
            var described = CreateDescribedThing();
            var id = described.Id;
            this._provider.Insert<DescribedThing, string>(described);
            var retrieved = this._provider.Get<DescribedThing, string>(id);
            var myBoolProperty = !retrieved.BoolProperty;
            retrieved.BoolProperty = myBoolProperty;
            var updated = this._provider.Update<DescribedThing, string>(retrieved);
            Assert.IsNotNull(updated.ModifiedBy);
        }

        [TestMethod]
        public void Modified_Entity_Change_Is_Persisted()
        {
            var described = CreateDescribedThing();
            var id = described.Id;
            this._provider.Insert<DescribedThing, string>(described);
            var retrieved = this._provider.Get<DescribedThing, string>(id);
            var newDescription = string.Format("{0} {1}", Product.Department(), DateTime.Now.TimeOfDay);
            retrieved.Description = newDescription;
            this._provider.Update<DescribedThing, string>(retrieved);
            using (var unitOfWork = new UnitOfWork())
            {
                retrieved = this._provider.Query<DescribedThing>(unitOfWork)
                    .FirstOrDefault(myDescribed => myDescribed.Id == id);
                if (retrieved != null)
                    Assert.AreEqual((object) retrieved.Description, newDescription);
                else
                    Assert.Fail("Description was not updated");
            }
        }

        [TestMethod]
        public void Queryable_Is_Case_Insensitive()
        {
            var described = CreateDescribedThing();
            var originalDescription = described.Description;
            this._provider.Insert<DescribedThing, string>(described);
            
            var describedUppercase = CreateDescribedThing();
            describedUppercase.Description = originalDescription.ToUpper();
            this._provider.Insert<DescribedThing, string>(describedUppercase);

            using (var unitOfWork = new UnitOfWork())
            {
                var retrievedCount = this._provider.Query<DescribedThing>(unitOfWork)
                    .Count(d => d.Description == originalDescription);
                var uppercaseCount = this._provider.Query<DescribedThing>(unitOfWork)
                    .Count(d => d.Description == originalDescription.ToUpper());
            
                // SQL server comparison
                Assert.IsTrue(retrievedCount == uppercaseCount);

                // .NET framework comparison using CaseInsensitiveQueryable:
                uppercaseCount = this._provider.Query<DescribedThing>()
                    .Where(d => d.Description == originalDescription.ToUpper())
                    .ToList()
                    .AsQueryable()
                    .AsCaseInsensitive()
                    .Count();
                Assert.IsTrue(retrievedCount == uppercaseCount);
            }
        }

        [TestMethod]
        public void Deleted_Entity_Is_Gone()
        {
            var described = CreateDescribedThing();
            var id = described.Id;
            this._provider.Insert<DescribedThing, string>(described);
            using (var unitOfWork = new UnitOfWork())
            {
                var retrieved = this._provider.Query<DescribedThing>(unitOfWork)
                    .FirstOrDefault(myDescribed => myDescribed.Id == id);
                Assert.IsNotNull(retrieved);
                this._provider.Delete<DescribedThing, string>(id);
                retrieved = this._provider.Query<DescribedThing>(unitOfWork)
                    .FirstOrDefault(myDescribed => myDescribed.Id == id);
                Assert.IsNull(retrieved);
            }
        }


        [TestMethod]
        public void Bulk_Inserted_Entities_Are_Present()
        {
            var things = new List<DescribedThing>();
            var randomValue = Guid.NewGuid().ToString();
            for (int i = 0; i < 10; i++)
            {
                var described = CreateDescribedThing();
                things.Add(described);
                described.Label = randomValue;
            }
            this._provider.BulkInsert<DescribedThing,string>(things);//.BulkInsert<DescribedThing, string>(things);
            using (var unitOfWork = new UnitOfWork())
            {
                var retrieved = this._provider.Query<DescribedThing>(unitOfWork)
                    .Where(myDescribed => myDescribed.Label == randomValue);
                Assert.AreEqual(retrieved.Count(), 10);
            }
        }

        [TestMethod]
        public void Bulk_Updated_Entities_Are_Changed()
        {
            this._provider.BulkUpdate<DescribedThing,string>(
                predicate: x => x.Description.StartsWith("h"), 
                propertyExpression: x => x.Label, 
                assignmentExpression: x => "");
            using (var unitOfWork = new UnitOfWork())
            {
                var things = this._provider.Query<DescribedThing>(unitOfWork)
                    .Where(x => x.Description.StartsWith("h"));
                Assert.IsTrue(things.All(x => x.Label == ""));
            }
        }

        [TestMethod]
        public void Bulk_Deleted_Entities_Are_Gone()
        {
            var describeds = new List<DescribedThing>();
            for (int i = 0; i < 20; i++)
            {
                describeds.Add(CreateDescribedThing());
            }
            using (var unitOfWork = new UnitOfWork())
            {

                var list = _provider.Query<DescribedThing>(unitOfWork)
                    .OrderBy(x => x.Id)
                    .Skip(10)
                    .Take(10)
                    .Select(x => x.Id)
                    .ToArray();

                var count = _provider.BulkDelete<DescribedThing>(x => list.Any(y => y == x.Id));
                Assert.AreEqual(list.Length, count);
                var query = _provider
                    .Query<DescribedThing>(unitOfWork)
                    .Where(x => Enumerable.Contains(list, x.Id))
                    .ToArray();
                Assert.IsFalse(query.Any());
            }
        }

        [TestMethod]
        public void Related_Entities_Are_Saved()
        {
            var transaction = CreateTransactionItem();
            var id = transaction.Id;
            this._provider.Insert<TransactionItem, string>(transaction);
            var retrieved = this._provider.Query<TransactionItem>(item => item.Modifications)
                .OrderByDescending(item => item.Id)
                .FirstOrDefault(item =>
                    item.Modifications.Any(amendment => amendment.TransactionItemId == id));
            if (retrieved != null)
                Assert.AreEqual(retrieved.Modifications.Count, 3);
        }

        [TestMethod]
        public void Related_Entities_Are_Deleted()
        {
            var transaction = CreateTransactionItem();
            var id = transaction.Id;
            this._provider.Insert<TransactionItem, string>(transaction);
            var retrieved = this._provider.Query<TransactionItem>(item => item.Modifications)
                .OrderByDescending(item => item.Id)
                .FirstOrDefault(item =>
                    item.Modifications.Any(amendment => amendment.TransactionItemId == id));
            if (retrieved != null)
            {
                var transactionItemAmendments = retrieved.Modifications.Skip(2).ToArray();
                foreach (var modification in transactionItemAmendments)
                {
                    // Note: You cannot simply remove the modification or set it to null;
                    // explicitly delete it instead:
                    this._provider.Delete<TransactionItemAmendment, string>(modification);
                }
            }
            // refresh 'retrieved' instance;
            retrieved = this._provider.Query<TransactionItem>(item => item.Modifications)
                .OrderByDescending(item => item.Id)
                .FirstOrDefault(item =>
                    item.Modifications.Any(amendment => amendment.TransactionItemId == id));
            if (retrieved != null)
                Assert.AreEqual(retrieved.Modifications.Count, 2);
        }

        [TestMethod]
        public void Two_Readers_Can_Read_Simultaneously()
        {
            var count = 10;
            var retrieved = GetThingsUsingTwoQueries(10);
            Assert.AreEqual(retrieved.Count, count * 2);
        }


        [TestMethod]
        public void Multiple_Concurrent_Queries_Can_Execute()
        {
            var count = 24;
            var retrieved = RunConcurrentQueriesAsync(count);
            Assert.AreEqual(Math.Pow(count, 2), retrieved.Count);
        }



        private DescribedThing CreateDescribedThing()
        {
            var described = new DescribedThing
            {
                Description = $"{Product.Department()} {DateTime.Now.TimeOfDay}",
                Label = Internet.DomainWord() + " " + DateTime.Now.Ticks,
                NullableIntProperty = Rnd.Next(10) < 6 ? null : (int?) Rnd.Next(1000000),
                BoolProperty = Rnd.Next(2) == 1,
                TextProperty = Lorem.Sentence(),
                Name = Person.FirstName()
            };
            return described;
        }

        /// <summary>
        ///     Create a TransactionItem with 3 child amendments.
        /// </summary>
        /// <returns></returns>
        private TransactionItem CreateTransactionItem()
        {
            var transaction = new TransactionItem
            {
                ItemAmount = this.Rnd.Next(1000)*0.1m,
                ItemText = Internet.UserName()
            };
            var reasons = new[] {"VOID", "RETURN", "DISCOUNT"};
            for (var i = 0; i < 3; i++)
            {
                var reason = reasons[i];
                var amendment = new TransactionItemAmendment
                {
                    TransactionItemId = transaction.Id,
                    ItemAmount = transaction.ItemAmount*(reason == "DISCOUNT" ? -0.25m : -1.0m),
                    Reason = reason
                };
                transaction.Modifications.Add(amendment);
            }
            return transaction;
        }

        private ICollection<DescribedThing> GetThingsUsingTwoQueries(int count)
        {
            var ret = new List<DescribedThing>();
            var query1 = _provider.Query<DescribedThing>()
                .Where(x => x.BoolProperty == true);
            var query2 = _provider.Query<DescribedThing>()
                .Where(x => x.BoolProperty == false);
            for (int i = 0; i < count; i++)
            {
                ret.Add(query1.OrderBy(x => x.Id).Skip(i).Take(1).FirstOrDefault());
                ret.Add(query2.OrderBy(x => x.Id).Skip(i).Take(1).FirstOrDefault());
            }
            return ret;
        }


        private ICollection<DescribedThing> RunConcurrentQueriesAsync(int count)
        {
            ICollection<DescribedThing> ret = new List<DescribedThing>();
            Task[] tasks = new Task[count];
            for (int i = 0; i < count; i++)
            {
                var i1 = i;
                tasks[i1] = Task.Factory.StartNew(() =>
                {
                    var thread = Thread.CurrentThread.ManagedThreadId;
                    try
                    {
                        var myBool = i1%2 == 0;
                        var query = _provider.Query<DescribedThing>()
                            .Where(x => x.BoolProperty == myBool);
                        query
                            .OrderBy(x => x.Id)
                            .Skip(i1)
                            .Take(count)
                            .ToList()
                            .ForEach(e => ret.Add(e));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Debug.WriteLine($"Exception from thread {thread}:");
                        Debug.WriteLine(e);
                    }
                });
            }
            Task.WaitAll(tasks);
            return ret;
        }
    }
}