﻿using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Deveroom.VisualStudio.SpecFlowConnector
{
    public static class ReflectionExtensions
    {
        public static T ReflectionCallMethod<T>(this object obj, string methodName, params object[] args)
        {
            return ReflectionCallMethod<T>(obj, methodName, args?.Select(a => a.GetType()).ToArray() ?? new Type[0], args);
        }

        public static void ReflectionCallMethod(this object obj, string methodName, Type[] parameterTypes, params object[] args)
        {
            ReflectionCallMethod<object>(obj, methodName, parameterTypes, args);
        }

        public static T ReflectionCallMethod<T>(this object obj, string methodName, Type[] parameterTypes, params object[] args)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var methodInfo = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
            if (methodInfo == null)
                throw new ArgumentException($"Cannot find method {methodName} on type {obj.GetType().FullName}");
            return (T)methodInfo.Invoke(obj, args);
        }

        public static T ReflectionCallStaticMethod<T>(this Type type, string methodName, Type[] parameterTypes, params object[] args)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
            if (methodInfo == null)
                throw new ArgumentException($"Cannot find method {methodName} on type {type.FullName}");
            return (T)methodInfo.Invoke(null, args);
        }

        public static T ReflectionGetField<T>(this object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new ArgumentException($"Cannot find field {fieldName} on type {obj.GetType().FullName}");
            return (T)fieldInfo.GetValue(obj);
        }

        public static void ReflectionSetField<T>(this object obj, string fieldName, T value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new ArgumentException($"Cannot find field {fieldName} on type {obj.GetType().FullName}");
            fieldInfo.SetValue(obj, value);
        }

        public static T ReflectionGetStaticField<T>(this Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var fieldInfo = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new ArgumentException($"Cannot find field {fieldName} on type {type.FullName}");
            return (T)fieldInfo.GetValue(null);
        }

        public static T ReflectionGetStaticProperty<T>(this Type type, string propertyName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var propertyInfo = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
                throw new ArgumentException($"Cannot find property {propertyName} on type {type.FullName}");
            return (T)propertyInfo.GetValue(null);
        }

        public static T ReflectionCreateInstance<T>(this Type type, Type[] parameterTypes, params object[] args)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
            if (constructorInfo == null)
                throw new ArgumentException($"Cannot find constructor on type {type.FullName}");
            return (T)constructorInfo.Invoke(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, args, null);
        }
    }
}
