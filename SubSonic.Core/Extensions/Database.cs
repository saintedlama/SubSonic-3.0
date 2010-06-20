// 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using SubSonic.DataProviders;
using SubSonic.Query;
using SubSonic.Schema;
using Constraint=SubSonic.Query.Constraint;
using SubSonic.Repository;

namespace SubSonic.Extensions
{
    public static class Database
    {
        /// <summary>
        /// Returns the SqlDbType for a give DbType
        /// </summary>
        /// <returns></returns>
        public static SqlDbType GetSqlDBType(this DbType dbType)
        {
            switch(dbType)
            {
                case DbType.AnsiString:
                    return SqlDbType.VarChar;
                case DbType.AnsiStringFixedLength:
                    return SqlDbType.Char;
                case DbType.Binary:
                    return SqlDbType.VarBinary;
                case DbType.Boolean:
                    return SqlDbType.Bit;
                case DbType.Byte:
                    return SqlDbType.TinyInt;
                case DbType.Currency:
                    return SqlDbType.Money;
                case DbType.Date:
                    return SqlDbType.DateTime;
                case DbType.DateTime:
                    return SqlDbType.DateTime;
                case DbType.Decimal:
                    return SqlDbType.Decimal;
                case DbType.Double:
                    return SqlDbType.Float;
                case DbType.Guid:
                    return SqlDbType.UniqueIdentifier;
                case DbType.Int16:
                    return SqlDbType.Int;
                case DbType.Int32:
                    return SqlDbType.Int;
                case DbType.Int64:
                    return SqlDbType.BigInt;
                case DbType.Object:
                    return SqlDbType.Variant;
                case DbType.SByte:
                    return SqlDbType.TinyInt;
                case DbType.Single:
                    return SqlDbType.Real;
                case DbType.String:
                    return SqlDbType.NVarChar;
                case DbType.StringFixedLength:
                    return SqlDbType.NChar;
                case DbType.Time:
                    return SqlDbType.DateTime;
                case DbType.UInt16:
                    return SqlDbType.Int;
                case DbType.UInt32:
                    return SqlDbType.Int;
                case DbType.UInt64:
                    return SqlDbType.BigInt;
                case DbType.VarNumeric:
                    return SqlDbType.Decimal;

                default:
                    return SqlDbType.VarChar;
            }
        }

        public static DbType GetDbType(Type type)
        {
            DbType result;

            if(type == typeof(Int32))
                result = DbType.Int32;
            else if (type == typeof(Int16))
                result = DbType.Int16;
            else if (type == typeof(Int64))
                result = DbType.Int64;
            else if(type == typeof(DateTime))
                result = DbType.DateTime;
            else if(type == typeof(float))
                result = DbType.Decimal;
            else if(type == typeof(decimal))
                result = DbType.Decimal;
            else if(type == typeof(double))
                result = DbType.Double;
            else if(type == typeof(Guid))
                result = DbType.Guid;
            else if(type == typeof(bool))
                result = DbType.Boolean;
            else if(type == typeof(byte[]))
                result = DbType.Binary;
            else
                result = DbType.String;

            return result;
        }

        /// <summary>
        /// Takes the properties of an object and turns them into SubSonic.Query.Constraint
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static List<Constraint> ToConstraintList(this object value)
        {
            var hashedSet = value.ToDictionary();
            SqlQuery query = new SqlQuery();
            foreach(string key in hashedSet.Keys)
            {
                if(query.Constraints.Count == 0)
                    query.Where(key).IsEqualTo(hashedSet[key]);
                else
                    query.And(key).IsEqualTo(hashedSet[key]);
            }
            return query.Constraints;
        }
        

        /// <summary>
        /// Creates a typed list from an IDataReader
        /// </summary>
        public static List<T> ToList<T>(this IDataReader rdr) where T : new()
        {
            // TODO: This instance could be cached static!
            var mapper = new DataMapper();
            return mapper.ToList<T>(rdr);
        }

        ///<summary>
        /// Builds a SubSonic UPDATE query from the passed-in object
        ///</summary>
        public static ISqlQuery ToUpdateQuery<T>(this T item, IDataProvider provider) where T : class, new()
        {
            Type type = typeof(T);
            var settings = item.ToDictionary();

            ITable tbl = provider.FindOrCreateTable<T>();

            Update<T> query = new Update<T>(tbl.Provider);
            if(item is IActiveRecord)
            {
                var ar = item as IActiveRecord;
                foreach(var dirty in ar.GetDirtyColumns())
                {
                    if(!dirty.IsPrimaryKey && !dirty.IsReadOnly)
                        query.Set(dirty.Name).EqualTo(settings[dirty.Name]);
                }
            }
            else
            {
                foreach(string key in settings.Keys)
                {
                    IColumn col = tbl.GetColumn(key);
                    if(col != null && !col.IsComputed)
                    {
                        if(!col.IsPrimaryKey && !col.IsReadOnly)
                            query.Set(col).EqualTo(settings[key]);
                    }
                }
            }

            //add the PK constraint
            Constraint c = new Constraint(ConstraintType.Where, tbl.PrimaryKey.Name)
                               {
                                   ParameterValue = settings[tbl.PrimaryKey.Name],
                                   ParameterName = tbl.PrimaryKey.Name,
                                   ConstructionFragment = tbl.PrimaryKey.Name
                               };
            query.Constraints.Add(c);

            return query;
        }

        ///<summary>
        /// Builds a SubSonic INSERT query from the passed-in object
        ///</summary>
        public static ISqlQuery ToInsertQuery<T>(this T item, IDataProvider provider) where T : class, new()
        {
            Type type = typeof(T);
            ITable tbl = provider.FindOrCreateTable<T>();
            Insert query = null;

            if(tbl != null)
            {
                var hashed = item.ToDictionary();
                query = new Insert(provider).Into<T>(tbl);
                foreach(string key in hashed.Keys)
                {
                    IColumn col = tbl.GetColumn(key);
                    if(col != null)
                    {
                        if(!col.AutoIncrement && !col.IsReadOnly && !(col.DefaultSetting != null && hashed[key] == null) && !col.IsComputed)
                            query.Value(col.QualifiedName, hashed[key], col.DataType);
                    }
                }
            }

            return query;
        }

        ///<summary>
        /// Builds a SubSonic DELETE query from the passed-in object
        ///</summary>
        public static ISqlQuery ToDeleteQuery<T>(this T item, IDataProvider provider) where T : class, new()
        {
            Type type = typeof(T);
            ITable tbl = provider.FindOrCreateTable<T>();
            var query = new Delete<T>(tbl, provider);
            if(tbl != null)
            {
                IColumn pk = tbl.PrimaryKey;
                var settings = item.ToDictionary();
                if(pk != null)
                {
                    var c = new Constraint(ConstraintType.Where, pk.Name)
                                {
                                    ParameterValue = settings[pk.Name],
                                    ParameterName = pk.Name,
                                    ConstructionFragment = pk.Name
                                };
                    query.Constraints.Add(c);
                }
                else
                    query.Constraints = item.ToConstraintList();
            }
            return query;
        }
    }
}
