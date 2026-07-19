using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Changes the surface longitude of an apsis.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationLongitude : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationLongitude";

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
