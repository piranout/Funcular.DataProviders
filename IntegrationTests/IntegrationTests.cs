#region File info
// *********************************************************************************************************
// Funcular.DataProviders>IntegrationTests>IntegrationTests.cs
// Created: 2015-07-02 3:52 PM
// Updated: 2015-07-02 5:49 PM
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
using System.Configuration;
using System.Linq;
using Funcular.DataProviders.EntityFramework;
using Funcular.DataProviders.IntegrationTests.Entities;
using Funcular.ExtensionMethods;
using Funcular.IdGenerators.Base36;
using Funcular.Ontology.Archetypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MockData;
#endregion


namespace Funcular.DataProviders.IntegrationTests
{
    [TestClass]
    public class IntegrationTests
    {
        private readonly Random _rnd = new Random();
        private Base36IdGenerator _base36;
        private EntityFrameworkProvider _provider;

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
            this._provider = new EntityFrameworkProvider(connectionString)
            {
                IsEntityType = type => type.IsSubclassOf(typeof (Createable<>))
            };
            this._provider.SetCurrentUser("Funcular\\Paul");
            Createable<string>.IdentityFunction = () => this._base36.NewId();
        }

        [TestMethod]
        public void Created_Entity_Is_Assigned_Id()
        {
            var described = GetEntityInstance();
            Assert.IsTrue(described.Id.HasValue() && described.Id.Length == 20);
        }

        [TestMethod]
        public void Created_Entity_Is_Assigned_DateCreatedUtc()
        {
            var described = GetEntityInstance();
            Assert.IsTrue(described.DateCreatedUtc != default(DateTime));
        }

        [TestMethod]
        public void Created_Entity_Is_Assigned_CreatedBy()
        {
            var described = GetEntityInstance();
            var inserted = this._provider.Insert<MyDescribed, string>(described);
            Assert.IsTrue(inserted.CreatedBy.HasValue());
        }

        [TestMethod]
        public void Created_Entity_Is_Persisted()
        {
            var described = GetEntityInstance();
            var id = described.Id;
            try
            {
                var inserted = this._provider.Insert<MyDescribed, string>(described);
                var fetched = this._provider.Get<MyDescribed, string>(id);
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
            var described = GetEntityInstance();
            var id = described.Id;
            this._provider.Insert<MyDescribed, string>(described);
            var retrieved = this._provider.Get<MyDescribed, string>(id);
            retrieved.MyBoolProperty = !retrieved.MyBoolProperty;
            var updated = this._provider.Update<MyDescribed, string>(retrieved);
            Assert.IsNotNull(updated.DateModifiedUtc);
        }

        [TestMethod]
        public void Modified_Entity_Is_Assigned_ModifiedBy()
        {
            var described = GetEntityInstance();
            var id = described.Id;
            this._provider.Insert<MyDescribed, string>(described);
            var retrieved = this._provider.Get<MyDescribed, string>(id);
            var myBoolProperty = !retrieved.MyBoolProperty;
            retrieved.MyBoolProperty = myBoolProperty;
            var updated = this._provider.Update<MyDescribed, string>(retrieved);
            Assert.IsNotNull(updated.ModifiedBy);
        }

        [TestMethod]
        public void Modified_Entity_Change_Is_Persisted()
        {
            var described = GetEntityInstance();
            var id = described.Id;
            this._provider.Insert<MyDescribed, string>(described);
            var retrieved = this._provider.Get<MyDescribed, string>(id);
            var newDescription = string.Format("{0} {1}", Product.Department(), DateTime.Now.TimeOfDay);
            retrieved.Description = newDescription;
            this._provider.Update<MyDescribed, string>(retrieved);
            retrieved = this._provider.Query<MyDescribed>()
                .FirstOrDefault(myDescribed => myDescribed.Id == id);
            if (retrieved != null)
                Assert.AreEqual((object) retrieved.Description, newDescription);
            else
            {
                Assert.Fail("Description was not updated");
            }
        }

        [TestMethod]
        public void Deleted_Entity_Is_Gone()
        {
            var described = GetEntityInstance();
            var id = described.Id;
            this._provider.Insert<MyDescribed, string>(described);
            var retrieved = this._provider.Query<MyDescribed>()
                .FirstOrDefault(myDescribed => myDescribed.Id == id);
            Assert.IsNotNull(retrieved);
            this._provider.Delete<MyDescribed, string>(id);
            retrieved = this._provider.Query<MyDescribed>()
                .FirstOrDefault(myDescribed => myDescribed.Id == id);
            Assert.IsNull(retrieved);
        }

        private MyDescribed GetEntityInstance()
        {
            var described = new MyDescribed
            {
                Description = string.Format("{0} {1}", Product.Department(), DateTime.Now.TimeOfDay),
                Label = Internet.DomainWord() + " " + DateTime.Now.Ticks,
                MyNullableIntProperty = this._rnd.Next(10) < 6 ? null : (int?) this._rnd.Next(1000000),
                MyBoolProperty = this._rnd.Next(2) == 1,
                MyTextProperty = Lorem.Sentence(),
                Name = Person.FirstName()
            };
            return described;
        }
    }
}