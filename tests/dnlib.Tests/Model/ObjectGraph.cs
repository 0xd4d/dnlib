// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace dnlib.Tests.Model {
	/// <summary>
	/// Finds all objects of dnlib's object model (types, members, signatures, method bodies, debug info, ...) that
	/// can be reached from a start object, eg. a module, by reading public properties and fields. Tests use it to check
	/// something for every object without listing all the classes.
	/// </summary>
	static class ObjectGraph {
		// Classes in other namespaces (eg. the metadata tables, the PE image) aren't part of the object model
		static readonly HashSet<string> modelNamespaces = new HashSet<string> {
			"dnlib.DotNet",
			"dnlib.DotNet.Emit",
			"dnlib.DotNet.Pdb",
		};

		/// <summary>
		/// <c>true</c> if it's a class of the object model
		/// </summary>
		public static bool IsModelObject(object value) {
			var type = value.GetType();
			return !type.IsValueType && type.Namespace is string @namespace && modelNamespaces.Contains(@namespace);
		}

		/// <summary>
		/// Returns the public instance properties that can be read without arguments
		/// </summary>
		public static IEnumerable<PropertyInfo> GetReadableProperties(Type type) {
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (property.CanRead && property.GetIndexParameters().Length == 0)
					yield return property;
			}
		}

		/// <summary>
		/// Returns the start objects and everything that can be reached from them
		/// </summary>
		/// <param name="startObjects">Eg. a module</param>
		/// <param name="isFollowed">Returns <c>true</c> if a property's value should be read</param>
		public static List<object> GetReachableObjects(IEnumerable<object> startObjects, Func<PropertyInfo, bool> isFollowed) {
			var found = new HashSet<object>(ReferenceComparer.Instance);
			var queue = new Queue<object>();
			foreach (var start in startObjects) {
				if (found.Add(start))
					queue.Enqueue(start);
			}

			while (queue.Count != 0) {
				var current = queue.Dequeue();
				foreach (var property in GetReadableProperties(current.GetType())) {
					if (!isFollowed(property))
						continue;
					object? value;
					try {
						value = property.GetValue(current, null);
					}
					catch (TargetInvocationException ex) {
						throw new InvalidOperationException($"{current.GetType().Name}.{property.Name} threw {ex.InnerException?.GetType().Name}", ex.InnerException);
					}
					foreach (var modelObject in GetModelObjects(value)) {
						if (found.Add(modelObject))
							queue.Enqueue(modelObject);
					}
				}
				// A few old classes have public fields, eg. Instruction.Operand
				foreach (var field in current.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
					foreach (var modelObject in GetModelObjects(field.GetValue(current))) {
						if (found.Add(modelObject))
							queue.Enqueue(modelObject);
					}
				}
			}
			return new List<object>(found);
		}

		// The value itself, or its elements if it's a list
		static IEnumerable<object> GetModelObjects(object? value) {
			if (value is null || value is string)
				yield break;
			if (IsModelObject(value))
				yield return value;
			// Some lists are classes of the object model themselves, eg. CustomAttributeCollection
			if (value is IEnumerable elements) {
				foreach (var element in elements) {
					if (element is not null && IsModelObject(element))
						yield return element;
				}
			}
		}

		sealed class ReferenceComparer : IEqualityComparer<object> {
			public static readonly ReferenceComparer Instance = new ReferenceComparer();
			bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);
			int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
		}
	}
}
