using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace RightEdge.DataStorage
{
	//	Source code copied from: http://stackoverflow.com/questions/2258310/get-an-idatareader-from-a-typed-list
	//	Also available at: http://spikes.codeplex.com/

	/// <summary>
	/// Creates an IDataReader over an instance of IEnumerable&lt;> or IEnumerable.
	/// Anonymous type arguments are acceptable.
	/// </summary>
	public class EnumerableDataReader : ObjectDataReader
	{
		private readonly IEnumerator _enumerator;
		private readonly Type _type;
		private object _current;

		public EnumerableDataReader(IEnumerable collection)
		{
			foreach (var intface in collection.GetType().GetInterfaces())
			{
				if (intface.IsGenericType && intface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					_type = intface.GetGenericArguments()[0];
					SetFields(_type);
					_enumerator = collection.GetEnumerator();
					return;
				}
			}

			throw new ArgumentException(
				"collection must be IEnumerable<>. Use other constructor for IEnumerable and specify type");
		}

		public EnumerableDataReader(IEnumerable collection, Type elementType)
			: base(elementType)
		{
			_type = elementType;
			_enumerator = collection.GetEnumerator();
		}

		public override object GetValue(int i)
		{
			if (i < 0 || i >= Fields.Count)
			{
				throw new IndexOutOfRangeException();
			}

			return Fields[i].Getter(_current);
		}

		public override bool Read()
		{
			bool returnValue = _enumerator.MoveNext();
			_current = returnValue ? _enumerator.Current : _type.IsValueType ? Activator.CreateInstance(_type) : null;
			return returnValue;
		}
	}

	public abstract class ObjectDataReader : IDataReader
	{
		protected bool Closed;
		protected IList<DynamicProperties.Property> Fields;

		protected ObjectDataReader()
		{
		}

		protected ObjectDataReader(Type elementType)
		{
			SetFields(elementType);
			Closed = false;
		}

		#region IDataReader Members

		public abstract object GetValue(int i);
		public abstract bool Read();

		#endregion

		#region Implementation of IDataRecord

		public int FieldCount
		{
			get { return Fields.Count; }
		}

		public virtual int GetOrdinal(string name)
		{
			for (int i = 0; i < Fields.Count; i++)
			{
				if (Fields[i].Info.Name == name)
				{
					return i;
				}
			}

			throw new IndexOutOfRangeException("name");
		}

		object IDataRecord.this[int i]
		{
			get { return GetValue(i); }
		}

		public virtual bool GetBoolean(int i)
		{
			return (Boolean)GetValue(i);
		}

		public virtual byte GetByte(int i)
		{
			return (Byte)GetValue(i);
		}

		public virtual char GetChar(int i)
		{
			return (Char)GetValue(i);
		}

		public virtual DateTime GetDateTime(int i)
		{
			return (DateTime)GetValue(i);
		}

		public virtual decimal GetDecimal(int i)
		{
			return (Decimal)GetValue(i);
		}

		public virtual double GetDouble(int i)
		{
			return (Double)GetValue(i);
		}

		public virtual Type GetFieldType(int i)
		{
			return Fields[i].Info.PropertyType;
		}

		public virtual float GetFloat(int i)
		{
			return (float)GetValue(i);
		}

		public virtual Guid GetGuid(int i)
		{
			return (Guid)GetValue(i);
		}

		public virtual short GetInt16(int i)
		{
			return (Int16)GetValue(i);
		}

		public virtual int GetInt32(int i)
		{
			return (Int32)GetValue(i);
		}

		public virtual long GetInt64(int i)
		{
			return (Int64)GetValue(i);
		}

		public virtual string GetString(int i)
		{
			return (string)GetValue(i);
		}

		public virtual bool IsDBNull(int i)
		{
			return GetValue(i) == null;
		}

		object IDataRecord.this[string name]
		{
			get { return GetValue(GetOrdinal(name)); }
		}


		public virtual string GetDataTypeName(int i)
		{
			return GetFieldType(i).Name;
		}


		public virtual string GetName(int i)
		{
			if (i < 0 || i >= Fields.Count)
			{
				throw new IndexOutOfRangeException("name");
			}
			return Fields[i].Info.Name;
		}

		public virtual int GetValues(object[] values)
		{
			int i = 0;
			for (; i < Fields.Count; i++)
			{
				if (values.Length <= i)
				{
					return i;
				}
				values[i] = GetValue(i);
			}
			return i;
		}

		public virtual IDataReader GetData(int i)
		{
			// need to think about this one
			throw new NotImplementedException();
		}

		public virtual long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			// need to keep track of the bytes got for each record - more work than i want to do right now
			// http://msdn.microsoft.com/en-us/library/system.data.idatarecord.getbytes.aspx
			throw new NotImplementedException();
		}

		public virtual long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			// need to keep track of the bytes got for each record - more work than i want to do right now
			// http://msdn.microsoft.com/en-us/library/system.data.idatarecord.getchars.aspx
			throw new NotImplementedException();
		}

		#endregion

		#region Implementation of IDataReader

		public virtual void Close()
		{
			Closed = true;
		}


		public virtual DataTable GetSchemaTable()
		{
			var dt = new DataTable();
			foreach (DynamicProperties.Property field in Fields)
			{
				dt.Columns.Add(new DataColumn(field.Info.Name, field.Info.PropertyType));
			}
			return dt;
		}

		public virtual bool NextResult()
		{
			throw new NotImplementedException();
		}


		public virtual int Depth
		{
			get { throw new NotImplementedException(); }
		}

		public virtual bool IsClosed
		{
			get { return Closed; }
		}

		public virtual int RecordsAffected
		{
			get
			{
				// assuming select only?
				return -1;
			}
		}

		#endregion

		#region Implementation of IDisposable

		public virtual void Dispose()
		{
			Fields = null;
		}

		#endregion

		protected void SetFields(Type elementType)
		{
			Fields = DynamicProperties.CreatePropertyMethods(elementType);
		}
	}

	/// <summary>
	/// Gets IL setters and getters for a property.
	/// 
	/// started with http://jachman.wordpress.com/2006/08/22/2000-faster-using-dynamic-method-calls/
	/// </summary>
	public static class DynamicProperties
	{
		#region Delegates

		public delegate object GenericGetter(object target);

		public delegate void GenericSetter(object target, object value);

		#endregion

		public static IList<Property> CreatePropertyMethods(Type T)
		{
			var returnValue = new List<Property>();

			foreach (PropertyInfo prop in T.GetProperties())
			{
				returnValue.Add(new Property(prop));
			}
			return returnValue;
		}


		public static IList<Property> CreatePropertyMethods<T>()
		{
			var returnValue = new List<Property>();

			foreach (PropertyInfo prop in typeof(T).GetProperties())
			{
				returnValue.Add(new Property(prop));
			}
			return returnValue;
		}


		/// <summary>
		/// Creates a dynamic setter for the property
		/// </summary>
		/// <param name="propertyInfo"></param>
		/// <returns></returns>
		public static GenericSetter CreateSetMethod(PropertyInfo propertyInfo)
		{
			/*
			* If there's no setter return null
			*/
			MethodInfo setMethod = propertyInfo.GetSetMethod();
			if (setMethod == null)
				return null;

			/*
			* Create the dynamic method
			*/
			var arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			var setter = new DynamicMethod(
				String.Concat("_Set", propertyInfo.Name, "_"),
				typeof(void), arguments, propertyInfo.DeclaringType);
			ILGenerator generator = setter.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
			generator.Emit(OpCodes.Ldarg_1);

			if (propertyInfo.PropertyType.IsClass)
				generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
			else
				generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);

			generator.EmitCall(OpCodes.Callvirt, setMethod, null);
			generator.Emit(OpCodes.Ret);

			/*
			* Create the delegate and return it
			*/
			return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
		}


		/// <summary>
		/// Creates a dynamic getter for the property
		/// </summary>
		/// <param name="propertyInfo"></param>
		/// <returns></returns>
		public static GenericGetter CreateGetMethod(PropertyInfo propertyInfo)
		{
			/*
			* If there's no getter return null
			*/
			MethodInfo getMethod = propertyInfo.GetGetMethod();
			if (getMethod == null)
				return null;

			/*
			* Create the dynamic method
			*/
			var arguments = new Type[1];
			arguments[0] = typeof(object);

			var getter = new DynamicMethod(
				String.Concat("_Get", propertyInfo.Name, "_"),
				typeof(object), arguments, propertyInfo.DeclaringType);
			ILGenerator generator = getter.GetILGenerator();
			generator.DeclareLocal(typeof(object));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
			generator.EmitCall(OpCodes.Callvirt, getMethod, null);

			if (!propertyInfo.PropertyType.IsClass)
				generator.Emit(OpCodes.Box, propertyInfo.PropertyType);

			generator.Emit(OpCodes.Ret);

			/*
			* Create the delegate and return it
			*/
			return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
		}

		#region Nested type: Property

		public class Property
		{
			public GenericGetter Getter;
			public PropertyInfo Info;
			public GenericSetter Setter;

			public Property(PropertyInfo info)
			{
				Info = info;
				Setter = CreateSetMethod(info);
				Getter = CreateGetMethod(info);
			}
		}

		#endregion

		///// <summary>
		///// An expression based Getter getter found in comments. untested.
		///// Q: i don't see a reciprocal setter expression?
		///// </summary>
		///// <typeparam name="T"></typeparam>
		///// <param name="propName"></param>
		///// <returns></returns>
		//public static Func<T> CreateGetPropValue<T>(string propName)
		//{
		//    var param = Expression.Parameter(typeof(object), "container");
		//    var func = Expression.Lambda(
		//    Expression.Convert(Expression.PropertyOrField(Expression.Convert(param, typeof(T)), propName), typeof(object)), param);
		//    return (Func<T>)func.Compile();
		//}
	}
}
