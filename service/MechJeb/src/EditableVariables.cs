using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;

namespace KRPC.MechJeb {
	internal static class EditableDouble {
		internal const string MechJebType = "MuMech.EditableDoubleMult";

		// Fields and methods
		private static PropertyInfo value;

		internal static void InitType(Type type) {
			value = type.GetCheckedProperty("Val");
		}

		/// <summary>
		/// Invokes MechJeb's <c>Get</c> operation.
		/// </summary>
		public static double Get(object instance) {
			return (double)value.GetValue(instance, null);
		}

		/// <summary>
		/// Invokes MechJeb's <c>Set</c> operation.
		/// </summary>
		public static void Set(object instance, double value) {
			EditableDouble.value.SetValue(instance, value, null);
		}

		// Helper methods for fields which create a new object every time they are changed in GUI
		/// <summary>
		/// Invokes MechJeb's <c>Get</c> operation.
		/// </summary>
		public static double Get(FieldInfo field, object instance) {
			return Get(field.GetValue(instance));
		}

		/// <summary>
		/// Invokes MechJeb's <c>Set</c> operation.
		/// </summary>
		public static void Set(FieldInfo field, object instance, double value) {
			Set(field.GetValue(instance), value);
		}
	}

	internal static class EditableInt {
		internal const string MechJebType = "MuMech.EditableInt";

		// Fields and methods
		private static FieldInfo value;
		private static FieldInfo text;

		internal static void InitType(Type type) {
			value = type.GetCheckedField("ValConfig");
			text = type.GetCheckedField("TextConfig");
		}

		/// <summary>
		/// Invokes MechJeb's <c>Get</c> operation.
		/// </summary>
		public static int Get(object instance) {
			return (int)value.GetValue(instance);
		}

		/// <summary>
		/// Invokes MechJeb's <c>Set</c> operation.
		/// </summary>
		public static void Set(object instance, int value) {
			EditableInt.value.SetValue(instance, value);
			text.SetValue(instance, value.ToString());
		}
	}

	internal static class MovingAverage {
		internal const string MechJebType = "MuMech.MovingAverage";

		// Fields and methods
		private static PropertyInfo value;

		internal static void InitType(Type type) {
			value = type.GetCheckedProperty("Value");
		}

		/// <summary>
		/// Invokes MechJeb's <c>Get</c> operation.
		/// </summary>
		public static double Get(object instance) {
			return (double)value.GetValue(instance, null);
		}
	}
}
