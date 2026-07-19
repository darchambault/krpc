using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// Provides access to MechJeb's <c>NodeExecutor</c> functionality.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class NodeExecutor : ComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleNodeExecutor";

		// Fields and methods
		private static FieldInfo autowarp;
		private static FieldInfo leadTimeField;
		private static FieldInfo toleranceField;

		private static MethodInfo executeOneNode;
		private static MethodInfo executeAllNodes;
		private static MethodInfo abort;

		// Instance objects
		private object leadTime;
		private object tolerance;
		private double compatibilityTolerance = 0.1;

		internal static new void InitType(Type type) {
			autowarp = type.GetCheckedField("Autowarp");
			leadTimeField = type.GetCheckedField("LeadTime");
			toleranceField = type.GetField("Tolerance");

			executeOneNode = type.GetCheckedMethod("ExecuteOneNode");
			executeAllNodes = type.GetCheckedMethod("ExecuteAllNodes");
			abort = type.GetCheckedMethod("Abort");
		}

		/// <summary>
		/// Gets whether the node executor is enabled.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.leadTime = leadTimeField.GetInstanceValue(instance);
			this.tolerance = toleranceField?.GetInstanceValue(instance);
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		[KRPCProperty]
		public override bool Enabled => base.Enabled;

		/// <summary>
		/// Gets or sets the value of <c>Autowarp</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool Autowarp {
			get => (bool)autowarp.GetValue(this.instance);
			set => autowarp.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>LeadTime</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double LeadTime {
			get => EditableDouble.Get(this.leadTime);
			set => EditableDouble.Set(this.leadTime, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>Tolerance</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double Tolerance {
			get => this.tolerance == null ? this.compatibilityTolerance : EditableDouble.Get(this.tolerance);
			set {
				if (this.tolerance == null)
					this.compatibilityTolerance = value;
				else
					EditableDouble.Set(this.tolerance, value);
			}
		}

		/// <summary>
		/// Invokes MechJeb's <c>ExecuteOneNode</c> operation.
		/// </summary>
		[KRPCMethod]
		public void ExecuteOneNode() {
			executeOneNode.Invoke(this.instance, new object[] { this });
		}

		/// <summary>
		/// Invokes MechJeb's <c>ExecuteAllNodes</c> operation.
		/// </summary>
		[KRPCMethod]
		public void ExecuteAllNodes() {
			executeAllNodes.Invoke(this.instance, new object[] { this });
		}

		/// <summary>
		/// Invokes MechJeb's <c>Abort</c> operation.
		/// </summary>
		[KRPCMethod]
		public void Abort() {
			abort.Invoke(this.instance, null);
		}
	}
}
