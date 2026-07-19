using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Changes the longitude of the ascending node.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationLan : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationLan";

		// Fields and methods
		private static FieldInfo timeSelector;

		internal static new void InitType(Type type) {
			timeSelector = GetTimeSelectorField(type);
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.InitTimeSelector(timeSelector);
		}
	}
}
