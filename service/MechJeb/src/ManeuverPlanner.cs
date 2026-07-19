using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.MechJeb.Maneuver;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// Provides access to MechJeb's <c>ManeuverPlanner</c> functionality.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class ManeuverPlanner : Module {
		internal const string MechJebType = "MuMech.MechJebModuleManeuverPlanner";

		// Fields and methods
		private static FieldInfo operationsField;

		// Instance objects
		private readonly Dictionary<string, Operation> operations = new Dictionary<string, Operation>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ManeuverPlanner"/> class.
		/// </summary>
		public ManeuverPlanner() {
			this.operations.Add("OperationApoapsis", new OperationApoapsis());
			this.operations.Add("OperationCircularize", new OperationCircularize());
			this.operations.Add("OperationCourseCorrection", new OperationCourseCorrection());
			this.operations.Add("OperationEllipticize", new OperationEllipticize());
			this.operations.Add("OperationInclination", new OperationInclination());
			this.operations.Add("OperationInterplanetaryTransfer", new OperationInterplanetaryTransfer());
			this.operations.Add("OperationKillRelVel", new OperationKillRelVel());
			this.operations.Add("OperationLambert", new OperationLambert());
			this.operations.Add("OperationLan", new OperationLan());
			this.operations.Add("OperationLongitude", new OperationLongitude());
			this.operations.Add("OperationMoonReturn", new OperationMoonReturn());
			this.operations.Add("OperationPeriapsis", new OperationPeriapsis());
			this.operations.Add("OperationPlane", new OperationPlane());
			this.operations.Add("OperationResonantOrbit", new OperationResonantOrbit());
			this.operations.Add("OperationSemiMajor", new OperationSemiMajor());
			this.operations.Add("OperationGeneric", new OperationTransfer());
		}

		internal static void InitType(Type type) {
			operationsField = type.GetCheckedField("_operation", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			Dictionary<string, object> operations = instance != null ? ((object[])operationsField.GetValue(instance)).ToDictionary(el => el.GetType().FullName, el => el) : new Dictionary<string, object>();

			foreach(KeyValuePair<string, Operation> p in this.operations) {
				string operationType = "MuMech." + p.Key;
				if(instance == null)
					p.Value.InitInstance(null);
				else if(operations.TryGetValue(operationType, out object operationInstance))
					p.Value.InitInstance(operationInstance);
				else {
					string error = "Operation " + p.Value.GetType().Name + " cannot be initialized";
					MechJebLogger.Error(error + ": " + operationType + " not found");
					MechJeb.errors.Add(error);
				}
			}
		}

		//TODO: OperationAdvancedTransfer

		/// <summary>
		/// Gets the value of <c>OperationApoapsis</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationApoapsis OperationApoapsis => (OperationApoapsis)this.operations["OperationApoapsis"];

		/// <summary>
		/// Gets the value of <c>OperationCircularize</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationCircularize OperationCircularize => (OperationCircularize)this.operations["OperationCircularize"];

		/// <summary>
		/// Gets the value of <c>OperationCourseCorrection</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationCourseCorrection OperationCourseCorrection => (OperationCourseCorrection)this.operations["OperationCourseCorrection"];

		/// <summary>
		/// Gets the value of <c>OperationEllipticize</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationEllipticize OperationEllipticize => (OperationEllipticize)this.operations["OperationEllipticize"];

		/// <summary>
		/// Gets the value of <c>OperationInclination</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationInclination OperationInclination => (OperationInclination)this.operations["OperationInclination"];

		/// <summary>
		/// Gets the value of <c>OperationInterplanetaryTransfer</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationInterplanetaryTransfer OperationInterplanetaryTransfer => (OperationInterplanetaryTransfer)this.operations["OperationInterplanetaryTransfer"];

		/// <summary>
		/// Gets the value of <c>OperationKillRelVel</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationKillRelVel OperationKillRelVel => (OperationKillRelVel)this.operations["OperationKillRelVel"];

		/// <summary>
		/// Gets the value of <c>OperationLambert</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationLambert OperationLambert => (OperationLambert)this.operations["OperationLambert"];

		/// <summary>
		/// Gets the value of <c>OperationLan</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationLan OperationLan => (OperationLan)this.operations["OperationLan"];

		/// <summary>
		/// Gets the value of <c>OperationLongitude</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationLongitude OperationLongitude => (OperationLongitude)this.operations["OperationLongitude"];

		/// <summary>
		/// Gets the value of <c>OperationMoonReturn</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationMoonReturn OperationMoonReturn => (OperationMoonReturn)this.operations["OperationMoonReturn"];

		/// <summary>
		/// Gets the value of <c>OperationPeriapsis</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationPeriapsis OperationPeriapsis => (OperationPeriapsis)this.operations["OperationPeriapsis"];

		/// <summary>
		/// Gets the value of <c>OperationPlane</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationPlane OperationPlane => (OperationPlane)this.operations["OperationPlane"];

		/// <summary>
		/// Gets the value of <c>OperationResonantOrbit</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationResonantOrbit OperationResonantOrbit => (OperationResonantOrbit)this.operations["OperationResonantOrbit"];

		/// <summary>
		/// Gets the value of <c>OperationSemiMajor</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationSemiMajor OperationSemiMajor => (OperationSemiMajor)this.operations["OperationSemiMajor"];

		/// <summary>
		/// Gets the value of <c>OperationTransfer</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public OperationTransfer OperationTransfer => (OperationTransfer)this.operations["OperationGeneric"];
	}
}
