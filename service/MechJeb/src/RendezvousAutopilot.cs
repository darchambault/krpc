using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// Provides access to MechJeb's <c>RendezvousAutopilot</c> functionality.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class RendezvousAutopilot : KRPCComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleRendezvousAutopilot";

		// Fields and methods
		private static FieldInfo desiredDistanceField;
		private static FieldInfo maxPhasingOrbitsField;
		private static FieldInfo status;

		// Instance objects
		private object desiredDistance;
		private object maxPhasingOrbits;

		internal static new void InitType(Type type) {
			desiredDistanceField = type.GetCheckedField("desiredDistance");
			maxPhasingOrbitsField = type.GetCheckedField("maxPhasingOrbits");
			status = type.GetCheckedField("status");
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.desiredDistance = desiredDistanceField.GetInstanceValue(instance);
			this.maxPhasingOrbits = maxPhasingOrbitsField.GetInstanceValue(instance);
		}

		/// <summary>
		/// Gets or sets the value of <c>DesiredDistance</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double DesiredDistance {
			get => EditableDouble.Get(this.desiredDistance);
			set => EditableDouble.Set(this.desiredDistance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>MaxPhasingOrbits</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double MaxPhasingOrbits {
			get => EditableDouble.Get(this.maxPhasingOrbits);
			set => EditableDouble.Set(this.maxPhasingOrbits, value);
		}

		/// <summary>
		/// Invokes MechJeb's <c>ToString</c> operation.
		/// </summary>
		[KRPCProperty]
		public string Status => status.GetValue(this.instance).ToString();
	}
}
