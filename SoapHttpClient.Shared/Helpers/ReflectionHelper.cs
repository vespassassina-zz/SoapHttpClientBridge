using System;
using System.Reflection;
using System.Globalization;

namespace System.Web.Services.Protocols
{

	public static class ReflectionHelper
	{

		public static object CreateInstance(Assembly assembly, string className, params object[] parameters)
		{
			try {
				return assembly.CreateInstance(className, false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance, null, parameters, null, null);
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}
		}

		public static object CreateInstance(string assemblyName, string className, params object[] parameters)
		{
			try {
				Assembly assembly = Assembly.Load(assemblyName);
				Type type = assembly.GetType(className);

				BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
				CultureInfo culture = null; // use InvariantCulture or other if you prefer
				object instantiatedType = Activator.CreateInstance(type, flags, null, parameters, culture);

				return instantiatedType;
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}

			//return assembly.CreateInstance (className, false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance, null, parameters, null, null);
		}

		public static Type GetTypeFromAssembly(string assemblyName, string className, bool arrayType = false)
		{
			try {
				Assembly assembly = Assembly.Load(assemblyName);
				Type type = assembly.GetType(className, false, true);

				if (type == null) {
					if (className == "Object") {
						type = typeof(Object);
					}
					if (className == "Boolean") {
						type = typeof(Boolean);
					}
					if (className == "String") {
						type = typeof(String);
					}
				}

				if (arrayType) {
					var array = Array.CreateInstance(type, 1);
					type = array.GetType();
				}

				//BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
				//CultureInfo culture = null; // use InvariantCulture or other if you prefer
				//object instantiatedType = Activator.CreateInstance (type, flags, null, parameters, culture);

				return type;
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}

			//return assembly.CreateInstance (className, false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance, null, parameters, null, null);
		}

		public static object GetPropertyValue(object instance, string propertyName)
		{
			try {
				Type classType = instance.GetType();
				PropertyInfo property = classType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);

				//iterate on base classes
				while (property == null && classType.BaseType != null) {
					classType = classType.BaseType;

					property = classType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);
				}

				return property.GetValue(instance);
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}
		}


		public static void SetPropertyValue(object instance, string propertyName, object propertyValue)
		{
			try {
				Type classType = instance.GetType();
				PropertyInfo property = classType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);

				//iterate on base classes
				while (property == null && classType.BaseType != null) {
					classType = classType.BaseType;

					property = classType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);
				}

				property.SetValue(instance, propertyValue);
			} catch (Exception ex) {
				Console.Write(ex.Message);
			}
		}

		public static object GetFieldValue(object instance, string fieldName)
		{
			try {
				Type classType = instance.GetType();
				FieldInfo field = classType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);

				//iterate on base classes
				while (field == null && classType.BaseType != null) {
					classType = classType.BaseType;

					field = classType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);
				}

				return field.GetValue(instance);
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}
		}

		public static void SetFieldValue(object instance, string fieldName, object fieldValue)
		{
			try {
				Type classType = instance.GetType();
				FieldInfo field = classType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);

				//iterate on base classes
				while (field == null && classType.BaseType != null) {
					classType = classType.BaseType;

					field = classType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static);
				}

				field.SetValue(instance, fieldValue);
			} catch (Exception ex) {
				Console.Write(ex.Message);
			}
		}

		public static object ExecuteMethod(object instance, string methodName, Type[] methodArgumentTypes = null, params object[] parameters)
		{
			try {
				MethodInfo method = null;
				Type classType = instance.GetType();

				if (methodArgumentTypes == null) {
					method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance);
				} else {
					method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance, Type.DefaultBinder, methodArgumentTypes, null);
				}

				while (method == null && classType.BaseType != null) {
					classType = classType.BaseType;
					if (methodArgumentTypes == null) {
						method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance);
					} else {
						method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance, Type.DefaultBinder, methodArgumentTypes, null);
					}
				}

				return method.Invoke(instance, parameters);
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}
		}

		public static object ExecuteMethod(object instance, string methodName, params object[] parameters)
		{
			try {
				MethodInfo method = null;
				Type classType = instance.GetType();

				//makes the array of params used to get the method
				Type[] methodArgumentTypes = new Type[parameters.Length];
				for (int i = 0; i < parameters.Length; i++) {
					methodArgumentTypes[i] = parameters[i].GetType();
				}

				//search the method
				method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance, Type.DefaultBinder, methodArgumentTypes, null);

				//iterative search into superclasses
				while (method == null && classType.BaseType != null) {
					classType = classType.BaseType;				
					method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance, Type.DefaultBinder, methodArgumentTypes, null);

				}

				//invoke
				return method.Invoke(instance, parameters);
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}
		}

		public static object ExecuteStaticMethod(Type classType, string methodName, Type[] methodArgumentTypes = null, params object[] parameters)
		{
			try {
				MethodInfo method = null;

				if (methodArgumentTypes == null) {
					method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static);
				} else {
					method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static, Type.DefaultBinder, methodArgumentTypes, null);
				}

				while (method == null && classType.BaseType != null && classType != typeof(object)) {
					classType = classType.BaseType;
					if (methodArgumentTypes == null) {
						method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static);
					} else {
						method = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static, Type.DefaultBinder, methodArgumentTypes, null);
					}
				}

				return method.Invoke(null, parameters);
			} catch (Exception ex) {
				Console.Write(ex.Message);
				throw;
			}
		}

	

	}
		
}


