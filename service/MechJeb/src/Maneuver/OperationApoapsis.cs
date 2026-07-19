using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to set a new apoapsis
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationApoapsis : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationApoapsis";

		// Fields and methods
		private static FieldInfo newApAField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object newApA;

		internal static new void InitType(Type type) {
			newApAField = type.GetCheckedField("NewApA");
			timeSelector = GetTimeSelectorField(type);
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.newApA = newApAField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		/// <summary>
		/// Gets or sets the value of <c>NewApoapsis</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double NewApoapsis {
			get => EditableDouble.Get(this.newApA);
			set => EditableDouble.Set(this.newApA, value);
		}
	}
}
