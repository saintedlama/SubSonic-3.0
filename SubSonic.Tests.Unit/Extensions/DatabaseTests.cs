﻿// 
//   SubSonic - http://subsonicproject.com
// 
//   The contents of this file are subject to the New BSD
//   License (the "License"); you may not use this file
//   except in compliance with the License. You may obtain a copy of
//   the License at http://www.opensource.org/licenses/bsd-license.php
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
using System;
using System.Linq;
using System.Data;
using SubSonic.DataProviders;
using SubSonic.Extensions;
using SubSonic.Query;
using SubSonic.Tests.Unit.TestClasses;
using Xunit;
using System.Collections.Generic;

namespace SubSonic.Tests.Unit.Extensions
{
    public class DatabaseTests
    {
        [Fact]
        public void GetSqlDbType_Should_Return_SqlType_Decimal_For_DbType_Decimal()
        {
            var dbType = DbType.Decimal;
            var sqlType = dbType.GetSqlDBType();

            Assert.IsType(typeof(SqlDbType), sqlType);
        }

        [Fact]
        public void ToConstraint_List_Should_Return_3_Constraints_For_TestObject()
        {
            var tester = new TestObject
                             {
                                 Test1 = 1,
                                 Test2 = "Test",
                                 Test3 = DateTime.Now.AddDays(1)
                             };

            var constraints = tester.ToConstraintList();
            Assert.Equal(3, constraints.Count);
            Assert.Equal(1, constraints[0].ParameterValue);
            Assert.Equal("Test1", constraints[0].ParameterName);
            Assert.Equal("Test", constraints[1].ParameterValue);
            Assert.Equal("Test2", constraints[1].ParameterName);
            Assert.True((DateTime)constraints[2].ParameterValue > DateTime.Now);
            Assert.Equal("Test3", constraints[2].ParameterName);
        }

        [Fact]
        public void ToSchemaTable_Should_Generate_Schema_Table_For_Product()
        {
            var provider = ProviderFactory.GetProvider("WestWind");
            var table = typeof(Product).ToSchemaTable(provider);

            Assert.NotNull(table);
            Assert.Equal(6, table.Columns.Count);
        }

        [Fact]
        public void ToSchemaTable_Should_Generate_Schema_Table_For_Product_WithPK_ID()
        {
            var provider = ProviderFactory.GetProvider("WestWind");
            var table = typeof(Product).ToSchemaTable(provider);

            Assert.Equal(table.PrimaryKey.Name, "ProductID");
        }

        [Fact]
        public void ToUpdateQuery_Should_Create_UpdateQuery_With_4_Settings_And_1_Constraint_For_Product()
        {
            var provider = ProviderFactory.GetProvider("WestWind");
            var id = Guid.NewGuid();
            Product p = new Product();
            p.ProductID = 1;
            p.ProductName = "Test";
            p.Sku = id;
            p.Discontinued = false;
            p.UnitPrice = 100.00M;

            var qry = p.ToUpdateQuery(provider);
            Assert.NotNull(qry);
            Assert.IsType(typeof(Update<Product>), qry);

            Update<Product> update = (Update<Product>)qry;
            Assert.Equal(5, update.Settings.Count);
            Assert.Equal(1, update.Constraints.Count);
            Assert.Equal(p.ProductID, update.Constraints[0].ParameterValue);
        }

        [Fact]
        public void ToInsertQuery_Should_Create_InsertQuery_With_4_Settings_And_1_Constraint_For_Product()
        {
            var provider = ProviderFactory.GetProvider("WestWind");
            Product p = new Product();
            p.ProductID = 1;
            p.ProductName = "Test";
            p.Sku = Guid.NewGuid();
            p.Discontinued = false;
            p.UnitPrice = 100.00M;

            var qry = p.ToInsertQuery(provider);
            Assert.NotNull(qry);
            Assert.IsType(typeof(Insert), qry);
            Insert insert = (Insert)qry;

            Assert.Equal(5, insert.Inserts.Count);
        }

        [Fact]
        public void ToDeleteQuery_Should_Create_DeleteQuery_With_1_Constraint_For_PKFor_Product()
        {
            var provider = ProviderFactory.GetProvider("WestWind");
            var id = Guid.NewGuid();
            Product p = new Product();
            p.ProductID = 1;
            p.ProductName = "Test";
            p.Sku = id;
            p.Discontinued = false;
            p.UnitPrice = 100.00M;

            var qry = p.ToDeleteQuery(provider);
            Assert.NotNull(qry);
            Assert.IsType(typeof(Delete<Product>), qry);
            Delete<Product> del = (Delete<Product>)qry;

            Assert.Equal(1, del.Constraints.Count);
            Assert.Equal(p.ProductID, del.Constraints[0].ParameterValue);
        }

        [Fact]
        public void ToEnumerable_Should_Create_Anonymous_Types_From_DataReader()
        {
            var items = new[] {
                new { Test1 = 1, Test2="1", Test3=new DateTime(2001, 1, 11) },
                new { Test1 = 1, Test2="1", Test3=new DateTime(2001, 1, 11) },
                new { Test1 = 1, Test2="1", Test3=new DateTime(2001, 1, 11) }
            };

            var reader = CreateDataReaderFrom(items);

            var enumerable = CreateEnumerable(reader, null, items);
            Assert.Equal(items, enumerable.ToArray());
        }

        [Fact]
        public void ToEnumerable_Should_Create_Anonymous_Types_From_DataReader_With_Empty_Columns()
        {
            var items = new[] {
                new { Test1 = 1, Test2="1", Test3=new DateTime(2001, 1, 11) },
                new { Test1 = 1, Test2="1", Test3=new DateTime(2001, 1, 11) },
                new { Test1 = 1, Test2="1", Test3=new DateTime(2001, 1, 11) }
            };

            var reader = CreateDataReaderFrom(items);

            var enumerable = CreateEnumerable(reader, new List<String>(), items);
            Assert.Equal(items, enumerable.ToArray());
        }

        [Fact]
        public void ToEnumerable_Should_Work_With_Core_System_Types_Int()
        {
            var items = new[] { 1, 2, 3 };

            var reader = CreateDataReaderFromScalar(items);
            var enumerable = CreateEnumerable(reader, new List<String>(), items);

            Assert.Equal(items, enumerable.ToArray());
        }

        [Fact]
        public void ToEnumerable_Should_Work_With_Core_System_Types_String()
        {
            var items = new[] { "1", "2", "3" };

            var reader = CreateDataReaderFromScalar(items);
            var enumerable = CreateEnumerable(reader, new List<String>(), items);

            Assert.Equal(items, enumerable.ToArray());
        }

        private static IEnumerable<T> CreateEnumerable<T>(IDataReader reader, List<string> columns, IEnumerable<T> itemsOftype)
        {
            return reader.ToEnumerable<T>(columns, null);
        }

        private static IDataReader CreateDataReaderFrom<T>(IEnumerable<T> items)
        {
            var props = typeof(T).GetProperties();
            DataTable dt = new DataTable();
            dt.Columns.AddRange(props.Select(p => new DataColumn(p.Name, p.PropertyType)).ToArray());

            foreach (var item in items)
            {
                var row = dt.NewRow();

                foreach (var prop in props)
                {
                    row[prop.Name] = prop.GetValue(item, null);
                }

                dt.Rows.Add(row);
            }

            return dt.CreateDataReader();
        }

        private static IDataReader CreateDataReaderFromScalar<T>(IEnumerable<T> items)
        {
            var props = typeof(T).GetProperties();
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("scalar", typeof(T)));

            foreach (var item in items)
            {
                var row = dt.NewRow();
                row[0] = item;
                dt.Rows.Add(row);
            }

            return dt.CreateDataReader();
        }
        
        #region Nested type: TestObject

        private class TestObject
        {
            public int Test1 { get; set; }
            public string Test2 { get; set; }
            public DateTime Test3 { get; set; }
        }

        #endregion
    }
}