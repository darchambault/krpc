using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// Provides access to MechJeb's <c>SmartRCS</c> functionality.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class SmartRCS : DisplayModule {
		internal new const string MechJebType = "MuMech.MechJebModuleSmartRcs";

		// Fields and methods
		private static FieldInfo target;
		private static FieldInfo autoDisableSmartRCS;

		private static MethodInfo engage;

		internal static new void InitType(Type type) {
			target = type.GetCheckedField("target");
			autoDisableSmartRCS = type.GetCheckedField("autoDisableSmartRCS");

			engage = type.GetCheckedMethod("Engage");
		}

		/// <summary>
		/// Gets or sets the value of <c>AutoDisableSmartRCS</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool AutoDisableSmartRCS {
			get => (bool)autoDisableSmartRCS.GetValue(this.instance);
			set => autoDisableSmartRCS.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>Mode</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public SmartRCSMode Mode {
			get => (SmartRCSMode)target.GetValue(this.instance);
			set {
				target.SetValue(this.instance, (int)value);
				engage.Invoke(this.instance, null);
			}
		}

		/// <summary>
		/// Gets the value of <c>RCSController</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public RCSController RCSController => MechJeb.RCSController;
	}

	/// <summary>
	/// Specifies values for <c>SmartRCSMode</c>.
	/// </summary>
	[KRPCEnum(Service = "MechJeb")]
	public enum SmartRCSMode {
		/// <summary>
		/// Selects <c>Off</c>.
		/// </summary>
		Off,
		/// <summary>
		/// Selects <c>ZeroRelativeVelocity</c>.
		/// </summary>
		ZeroRelativeVelocity,
		/// <summary>
		/// Selects <c>ZeroVelocity</c>.
		/// </summary>
		ZeroVelocity
	}
}
