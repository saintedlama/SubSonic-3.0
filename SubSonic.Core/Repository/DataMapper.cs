using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using SubSonic.Extensions;
using System.ComponentModel;
using System.Reflection;
using SubSonic.Schema;

namespace SubSonic.Repository
{
    public class DataMapper
    {
        private Action<object> _onItemCreated;

        public void OnItemCreated(Action<object> onItemCreated)
        {
            _onItemCreated = onItemCreated;
        }

        /// <summary>
        /// Creates a typed list from an IDataReader
        /// </summary>
        public List<T> ToList<T>(IDataReader rdr) where T : new()
        {
            List<T> result = new List<T>();
            Type iType = typeof(T);

            //set the values        
            while (rdr.Read())
            {
                T item = new T();
                Load(rdr, item, null);//mike added null to match ColumnNames
                result.Add(item);
            }
            return result;
        }


        /// <summary>
        /// Coerces an IDataReader to load an enumerable of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rdr"></param>
        /// <param name="columnNames"></param>
        public IEnumerable<T> ToEnumerable<T>(IDataReader rdr, List<string> columnNames)
        {//mike added ColumnNames

            List<T> result = new List<T>();
            while (rdr.Read())
            {
                T instance = default(T);
                var type = typeof(T);
                if (type.Name.Contains("AnonymousType"))
                {

                    //this is an anon type and it has read-only fields that are set
                    //in a constructor. So - read the fields and build it
                    //http://stackoverflow.com/questions/478013/how-do-i-create-and-access-a-new-instance-of-an-anonymous-class-passed-as-a-param
                    var properties = TypeDescriptor.GetProperties(instance);
                    int objIdx = 0;
                    object[] objArray = new object[properties.Count];

                    foreach (PropertyDescriptor info in properties)
                        objArray[objIdx++] = rdr[info.Name];

                    result.Add((T)Activator.CreateInstance(instance.GetType(), objArray));
                }
                //TODO: there has to be a better way to work with the type system
                else if (IsCoreSystemType(type))
                {
                    instance = (T)rdr.GetValue(0).ChangeTypeTo(type);
                    result.Add(instance);
                }
                else
                    instance = Activator.CreateInstance<T>();

                //do we have a parameterless constructor?
                Load(rdr, instance, columnNames);//mike added ColumnNames
                result.Add(instance);
            }
            return result.AsEnumerable();
        }

        public void Load<T>(IDataReader rdr, T item)
        {
            Load<T>(rdr, item, new List<string>());
        }

        /// <summary>
        /// Coerces an IDataReader to try and load an object using name/property matching
        /// </summary>
        public void Load<T>(IDataReader rdr, T item, List<string> ColumnNames) //mike added ColumnNames
        {
            Type iType = typeof(T);

            PropertyInfo[] cachedProps = iType.GetProperties();
            FieldInfo[] cachedFields = iType.GetFields();

            PropertyInfo currentProp;
            FieldInfo currentField = null;

            for (int i = 0; i < rdr.FieldCount; i++)
            {
                string pName = rdr.GetName(i);
                currentProp = cachedProps.SingleOrDefault(x => x.Name.Equals(pName, StringComparison.InvariantCultureIgnoreCase));

                //mike if the property is null and ColumnNames has data then look in ColumnNames for match
                if (currentProp == null && ColumnNames != null && ColumnNames.Count > i)
                {
                    currentProp = cachedProps.First(x => x.Name == ColumnNames[i]);
                }

                //if the property is null, likely it's a Field
                if (currentProp == null)
                    currentField = cachedFields.SingleOrDefault(x => x.Name.Equals(pName, StringComparison.InvariantCultureIgnoreCase));

                if (currentProp != null && !DBNull.Value.Equals(rdr.GetValue(i)))
                {
                    Type valueType = rdr.GetValue(i).GetType();
                    if (valueType == typeof(Boolean))
                    {
                        string value = rdr.GetValue(i).ToString();
                        currentProp.SetValue(item, value == "1" || value == "True", null);
                    }
                    else if (currentProp.PropertyType == typeof(Guid))
                    {
                        currentProp.SetValue(item, rdr.GetGuid(i), null);
                    }
                    else if (Objects.IsNullableEnum(currentProp.PropertyType))
                    {
                        var nullEnumObjectValue = Enum.ToObject(Nullable.GetUnderlyingType(currentProp.PropertyType), rdr.GetValue(i));
                        currentProp.SetValue(item, nullEnumObjectValue, null);
                    }
                    else if (currentProp.PropertyType.IsEnum)
                    {
                        var enumValue = Enum.ToObject(currentProp.PropertyType, rdr.GetValue(i));
                        currentProp.SetValue(item, enumValue, null);
                    }
                    else
                    {

                        var val = rdr.GetValue(i);
                        var valType = val.GetType();
                        //try to assign it
                        if (currentProp.PropertyType.IsAssignableFrom(valueType))
                        {
                            currentProp.SetValue(item, val, null);
                        }
                        else
                        {
                            currentProp.SetValue(item, val.ChangeTypeTo(currentProp.PropertyType), null);
                        }
                    }
                }
                else if (currentField != null && !DBNull.Value.Equals(rdr.GetValue(i)))
                {
                    Type valueType = rdr.GetValue(i).GetType();
                    if (valueType == typeof(Boolean))
                    {
                        string value = rdr.GetValue(i).ToString();
                        currentField.SetValue(item, value == "1" || value == "True");
                    }
                    else if (currentField.FieldType == typeof(Guid))
                    {
                        currentField.SetValue(item, rdr.GetGuid(i));
                    }
                    else if (Objects.IsNullableEnum(currentField.FieldType))
                    {
                        var nullEnumObjectValue = Enum.ToObject(Nullable.GetUnderlyingType(currentField.FieldType), rdr.GetValue(i));
                        currentField.SetValue(item, nullEnumObjectValue);
                    }
                    else
                        currentField.SetValue(item, rdr.GetValue(i).ChangeTypeTo(valueType));
                }
            }

            if (item is IActiveRecord)
            {
                var arItem = (IActiveRecord)item;
                arItem.SetIsLoaded(true);
                arItem.SetIsNew(false);

            }
        }


        /// <summary>
        /// Loads a single primitive value type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void LoadValueType<T>(IDataReader rdr, ref T item)
        {
            Type iType = typeof(T);
            //thanks to Pascal LaCroix for the help here...

            if (iType.IsValueType)
            {
                // We assume only one field
                if (iType == typeof(Int16) || iType == typeof(Int32) || iType == typeof(Int64))
                    item = (T)Convert.ChangeType(rdr.GetValue(0), iType);
                else
                    item = (T)rdr.GetValue(0);
            }
        }

        /// <summary>
        /// Toes the type of the enumerable value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rdr">The IDataReader to read from.</param>
        /// <returns></returns>
        public IEnumerable<T> ToEnumerableValueType<T>(IDataReader rdr)
        {
            //thanks to Pascal LaCroix for the help here...
            List<T> result = new List<T>();
            while (rdr.Read())
            {
                var instance = Activator.CreateInstance<T>();
                LoadValueType(rdr, ref instance);
                result.Add(instance);
            }
            return result.AsEnumerable();
        }

        /// <summary>
        /// Determines whether [is core system type] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// 	<c>true</c> if [is core system type] [the specified type]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsCoreSystemType(Type type)
        {
            return type == typeof(string) ||
                   type == typeof(Int16) ||
                   type == typeof(Int16?) ||
                   type == typeof(Int32) ||
                   type == typeof(Int32?) ||
                   type == typeof(Int64) ||
                   type == typeof(Int64?) ||
                   type == typeof(decimal) ||
                   type == typeof(decimal?) ||
                   type == typeof(double) ||
                   type == typeof(double?) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTime?) ||
                   type == typeof(Guid) ||
                   type == typeof(Guid?) ||
                   type == typeof(bool) ||
                   type == typeof(bool?);
        }
    }
}
