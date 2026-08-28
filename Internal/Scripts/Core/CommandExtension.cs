using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MobileConsole
{
	public static class CommandExtension
	{
        static Type[] _supportedFieldTypes = new Type[]
        {
            typeof(bool),
            typeof(sbyte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
			typeof(Enum)
        };

		static bool IsFiedTypeSupported(this Type type)
		{
			Type nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null)
			{
				return nullableType == typeof(bool) || nullableType.IsEnum;
			}

			return type.IsEnum || Array.IndexOf(_supportedFieldTypes, type) >= 0;
		}

		public static void CacheVariableInfos<T>(this Command command)
		{
			CacheVariableInfos(command, typeof(T));
		}

		public static void CacheVariableInfos(this Command command, Type type)
		{
			// Not DeclaredOnly: option classes commonly inherit shared fields from a base class
			// (e.g. BaseBugReporter.Options). Fields declared on Command itself (info, refreshUI)
			// are plumbing, not user-editable options, so they're filtered out below.
			FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(fieldInfo => fieldInfo.DeclaringType != typeof(Command))
				.ToArray();
			List<VariableInfo> variableInfos = new List<VariableInfo>();

			foreach (var fieldInfo in fieldInfos)
			{
                // Check if type is unsupported
                if (!fieldInfo.FieldType.IsFiedTypeSupported())
                {
					Debug.LogWarningFormat("Field type is unsupported: {0} - {1}({2})", type.Name, fieldInfo.Name, fieldInfo.FieldType.Name);
					continue;
                }

				// Create Variable info
                VariableInfo variableInfo = new VariableInfo();
				variableInfo.fieldInfo = fieldInfo;
				
				VariableAttribute attribute;
				if (fieldInfo.HasAttribute<VariableAttribute>(out attribute))
				{
					if (!string.IsNullOrEmpty(attribute.OnValueChanged))
					{
						variableInfo.callbackMethodInfo = type.GetMethod(attribute.OnValueChanged, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
						if (variableInfo.callbackMethodInfo == null)
						{
							Debug.LogWarningFormat("Could not found method <b>{0}</b> in <b>{1}</b>", attribute.OnValueChanged, type.Name);
						}
					}
				}

                variableInfos.Add(variableInfo);
			}

            command.info.variableInfos = variableInfos.ToArray();
		}

		public static void CacheCustomButtonInfos(this Command command, Type type, Type attributeType)
		{
			command.info.customButtonInfos = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
											.Where(method => method.HasAttribute(attributeType))
											.ToArray();
		}

		public static void LoadSavedValue(this Command command)
		{
			foreach (var variableInfo in command.info.variableInfos)
			{
				string savedKey = command.GetKey(variableInfo);
				if (PlayerPrefs.HasKey(savedKey))
				{
					string value = PlayerPrefs.GetString(savedKey);
					Type fieldType = variableInfo.fieldInfo.FieldType;
					Type nullableType = Nullable.GetUnderlyingType(fieldType);
					Type valueType = nullableType ?? fieldType;

					if (nullableType != null && string.IsNullOrEmpty(value))
					{
						variableInfo.fieldInfo.SetValue(command, null);
					}
					else if (valueType.IsEnum)
					{
						// In case the enum value was changed and not exist anymore, an exception will be thrown. We reset the value to 0
						try
						{
							variableInfo.fieldInfo.SetValue(command, Enum.Parse(valueType, value));
						}
						catch
						{
							Debug.LogWarningFormat("The enum value [{0}] is not exist anymore, set it to default (0)", value);
							variableInfo.fieldInfo.SetValue(command, Enum.ToObject(valueType, 0));
						}
					}
					else
					{
						try
						{
							variableInfo.fieldInfo.SetValue(command, Convert.ChangeType(value, valueType));
						}
						catch {}
					}

					// Value has changed
					if (variableInfo.callbackMethodInfo != null)
					{
						try
						{
							variableInfo.callbackMethodInfo.Invoke(command, null);
						}
						catch (Exception e)
						{
							Debug.LogException(e);
						}
					}
				}
			}
		}

		public static void SaveAllFieldInfos(this Command command)
		{
			foreach (var variableInfo in command.info.variableInfos)
			{
				command.SaveVariabledInfo(variableInfo);
			}
		}

		public static void SaveVariabledInfo(this Command command, VariableInfo variableInfo)
		{
			string savedKey = command.GetKey(variableInfo);
			object fieldValue = variableInfo.fieldInfo.GetValue(command);
			string value = fieldValue != null ? (string)Convert.ChangeType(fieldValue, typeof(string)) : string.Empty;
			PlayerPrefs.SetString(savedKey, value);
		}

		static string GetKey(this Command command, VariableInfo variableInfo)
		{
			return command.GetType().ToString() + "_" + variableInfo.fieldInfo.Name;
		}
	}
}
