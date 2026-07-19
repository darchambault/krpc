using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// Provides access to MechJeb's <c>RCSController</c> functionality.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class RCSController : ComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleRCSController";

		// Fields and methods
		private static FieldInfo rcsThrottle;
		private static FieldInfo rcsForRotation;

		internal static new void InitType(Type type) {
			rcsThrottle = type.GetCheckedField("rcsThrottle");
			rcsForRotation = type.GetCheckedField("rcsForRotation");
		}

		/// <summary>
		/// Gets or sets the value of <c>RCSThrottle</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool RCSThrottle {
			get => (bool)rcsThrottle.GetValue(this.instance);
			set => rcsThrottle.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RCSForRotation</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool RCSForRotation {
			get => (bool)rcsForRotation.GetValue(this.instance);
			set => rcsForRotation.SetValue(this.instance, value);
		}
	}
}
