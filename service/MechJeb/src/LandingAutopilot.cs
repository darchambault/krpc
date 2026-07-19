using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// The Landing Guidance module provides targeted and non-targeted landing autopilot.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class LandingAutopilot : AutopilotModule {
		internal new const string MechJebType = "MuMech.MechJebModuleLandingAutopilot";

		// Fields and methods
		private static FieldInfo touchdownSpeedField;
		private static FieldInfo deployGears;
		private static FieldInfo limitGearsStageField;
		private static FieldInfo deployChutes;
		private static FieldInfo limitChutesStageField;
		private static FieldInfo rcsAdjustment;

		private static MethodInfo landAtPositionTarget;
		private static MethodInfo landUntargeted;
		private static MethodInfo stopLanding;

		// Instance objects
		private object touchdownSpeed;
		private object limitGearsStage;
		private object limitChutesStage;

		internal static new void InitType(Type type) {
			touchdownSpeedField = type.GetCheckedField("TouchdownSpeed");
			deployGears = type.GetCheckedField("DeployGears");
			limitGearsStageField = type.GetCheckedField("LimitGearsStage");
			deployChutes = type.GetCheckedField("DeployChutes");
			limitChutesStageField = type.GetCheckedField("LimitChutesStage");
			rcsAdjustment = type.GetCheckedField("RCSAdjustment");

			landAtPositionTarget = type.GetCheckedMethod("LandAtPositionTarget");
			landUntargeted = type.GetCheckedMethod("LandUntargeted");
			stopLanding = type.GetCheckedMethod("StopLanding");
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.touchdownSpeed = touchdownSpeedField.GetInstanceValue(instance);
			this.limitGearsStage = limitGearsStageField.GetInstanceValue(instance);
			this.limitChutesStage = limitChutesStageField.GetInstanceValue(instance);
		}

		/// <summary>
		/// Gets or sets the value of <c>TouchdownSpeed</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double TouchdownSpeed {
			get => EditableDouble.Get(this.touchdownSpeed);
			set => EditableDouble.Set(this.touchdownSpeed, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>DeployGears</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool DeployGears {
			get => (bool)deployGears.GetValue(this.instance);
			set => deployGears.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>LimitGearsStage</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public int LimitGearsStage {
			get => EditableInt.Get(this.limitGearsStage);
			set => EditableInt.Set(this.limitGearsStage, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>DeployChutes</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool DeployChutes {
			get => (bool)deployChutes.GetValue(this.instance);
			set => deployChutes.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>LimitChutesStage</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public int LimitChutesStage {
			get => EditableInt.Get(this.limitChutesStage);
			set => EditableInt.Set(this.limitChutesStage, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RcsAdjustment</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool RcsAdjustment {
			get => (bool)rcsAdjustment.GetValue(this.instance);
			set => rcsAdjustment.SetValue(this.instance, value);
		}

		/// <summary>
		/// Invokes MechJeb's <c>LandAtPositionTarget</c> operation.
		/// </summary>
		[KRPCMethod]
		public void LandAtPositionTarget() {
			landAtPositionTarget.Invoke(this.instance, new object[] { this });
		}

		/// <summary>
		/// Invokes MechJeb's <c>LandUntargeted</c> operation.
		/// </summary>
		[KRPCMethod]
		public void LandUntargeted() {
			landUntargeted.Invoke(this.instance, new object[] { this });
		}

		/// <summary>
		/// Invokes MechJeb's <c>StopLanding</c> operation.
		/// </summary>
		[KRPCMethod]
		public void StopLanding() {
			stopLanding.Invoke(this.instance, null);
		}
	}
}
