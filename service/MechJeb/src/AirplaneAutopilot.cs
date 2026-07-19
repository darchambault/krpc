using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {

	/// <summary>
	/// Provides access to MechJeb's <c>AirplaneAutopilot</c> functionality.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class AirplaneAutopilot : KRPCComputerModule {
		internal new const string MechJebType = "MuMech.MechJebModuleAirplaneAutopilot";

		// Fields and methods
		private static FieldInfo headingHoldEnabled;
		private static FieldInfo altitudeHoldEnabled;
		private static FieldInfo vertSpeedHoldEnabled;
		private static FieldInfo rollHoldEnabled;
		private static FieldInfo speedHoldEnabled;

		private static FieldInfo altitudeTarget;
		private static FieldInfo headingTarget;
		private static FieldInfo rollTarget;
		private static FieldInfo speedTarget;
		private static FieldInfo vertSpeedTarget;
		private static FieldInfo bankAngle;

		private static FieldInfo accKpField;
		private static FieldInfo accKiField;
		private static FieldInfo accKdField;

		private static FieldInfo pitKpField;
		private static FieldInfo pitKiField;
		private static FieldInfo pitKdField;

		private static FieldInfo rolKpField;
		private static FieldInfo rolKiField;
		private static FieldInfo rolKdField;

		private static FieldInfo yawKpField;
		private static FieldInfo yawKiField;
		private static FieldInfo yawKdField;

		private static FieldInfo yawLimitField;
		private static FieldInfo rollLimitField;
		private static FieldInfo pitchUpLimitField;
		private static FieldInfo pitchDownLimitField;

		// Instance objects
		private object accKp;
		private object accKi;
		private object accKd;

		private object pitKp;
		private object pitKi;
		private object pitKd;

		private object rolKp;
		private object rolKi;
		private object rolKd;

		private object yawKp;
		private object yawKi;
		private object yawKd;

		private object yawLimit;
		private object rollLimit;
		private object pitchUpLimit;
		private object pitchDownLimit;

		internal static new void InitType(Type type) {
			headingHoldEnabled = type.GetCheckedField("HeadingHoldEnabled");
			altitudeHoldEnabled = type.GetCheckedField("AltitudeHoldEnabled");
			vertSpeedHoldEnabled = type.GetCheckedField("VertSpeedHoldEnabled");
			rollHoldEnabled = type.GetCheckedField("RollHoldEnabled");
			speedHoldEnabled = type.GetCheckedField("SpeedHoldEnabled");

			altitudeTarget = type.GetCheckedField("AltitudeTarget");
			headingTarget = type.GetCheckedField("HeadingTarget");
			rollTarget = type.GetCheckedField("RollTarget");
			speedTarget = type.GetCheckedField("SpeedTarget");
			vertSpeedTarget = type.GetCheckedField("VertSpeedTarget");
			bankAngle = type.GetCheckedField("BankAngle");

			accKpField = type.GetCheckedField("AccKp");
			accKiField = type.GetCheckedField("AccKi");
			accKdField = type.GetCheckedField("AccKd");

			pitKpField = type.GetCheckedField("PitKp");
			pitKiField = type.GetCheckedField("PitKi");
			pitKdField = type.GetCheckedField("PitKd");

			rolKpField = type.GetCheckedField("RolKp");
			rolKiField = type.GetCheckedField("RolKi");
			rolKdField = type.GetCheckedField("RolKd");

			yawKpField = type.GetCheckedField("YawKp");
			yawKiField = type.GetCheckedField("YawKi");
			yawKdField = type.GetCheckedField("YawKd");

			yawLimitField = type.GetCheckedField("YawLimit");
			rollLimitField = type.GetCheckedField("RollLimit");
			pitchUpLimitField = type.GetCheckedField("PitchUpLimit");
			pitchDownLimitField = type.GetCheckedField("PitchDownLimit");
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);
			this.accKp = accKpField.GetInstanceValue(instance);
			this.accKi = accKiField.GetInstanceValue(instance);
			this.accKd = accKdField.GetInstanceValue(instance);

			this.pitKp = pitKpField.GetInstanceValue(instance);
			this.pitKi = pitKiField.GetInstanceValue(instance);
			this.pitKd = pitKdField.GetInstanceValue(instance);

			this.rolKp = rolKpField.GetInstanceValue(instance);
			this.rolKi = rolKiField.GetInstanceValue(instance);
			this.rolKd = rolKdField.GetInstanceValue(instance);

			this.yawKp = yawKpField.GetInstanceValue(instance);
			this.yawKi = yawKiField.GetInstanceValue(instance);
			this.yawKd = yawKdField.GetInstanceValue(instance);

			this.yawLimit = yawLimitField.GetInstanceValue(instance);
			this.rollLimit = rollLimitField.GetInstanceValue(instance);
			this.pitchUpLimit = pitchUpLimitField.GetInstanceValue(instance);
			this.pitchDownLimit = pitchDownLimitField.GetInstanceValue(instance);
		}

		/// <summary>
		/// Gets or sets the value of <c>HeadingHoldEnabled</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool HeadingHoldEnabled {
			get => (bool)headingHoldEnabled.GetValue(this.instance);
			set => headingHoldEnabled.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>AltitudeHoldEnabled</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool AltitudeHoldEnabled {
			get => (bool)altitudeHoldEnabled.GetValue(this.instance);
			set => altitudeHoldEnabled.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>VertSpeedHoldEnabled</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool VertSpeedHoldEnabled {
			get => (bool)vertSpeedHoldEnabled.GetValue(this.instance);
			set => vertSpeedHoldEnabled.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RollHoldEnabled</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool RollHoldEnabled {
			get => (bool)rollHoldEnabled.GetValue(this.instance);
			set => rollHoldEnabled.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>SpeedHoldEnabled</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public bool SpeedHoldEnabled {
			get => (bool)speedHoldEnabled.GetValue(this.instance);
			set => speedHoldEnabled.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>AltitudeTarget</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double AltitudeTarget {
			get => (double)altitudeTarget.GetValue(this.instance);
			set => altitudeTarget.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>HeadingTarget</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double HeadingTarget {
			get => (double)headingTarget.GetValue(this.instance);
			set => headingTarget.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RollTarget</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double RollTarget {
			get => (double)rollTarget.GetValue(this.instance);
			set => rollTarget.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>SpeedTarget</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double SpeedTarget {
			get => (double)speedTarget.GetValue(this.instance);
			set => speedTarget.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>VertSpeedTarget</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double VertSpeedTarget {
			get => (double)vertSpeedTarget.GetValue(this.instance);
			set => vertSpeedTarget.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>BankAngle</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double BankAngle {
			get => (double)bankAngle.GetValue(this.instance);
			set => bankAngle.SetValue(this.instance, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>AccKp</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double AccKp {
			get => EditableDouble.Get(this.accKp);
			set => EditableDouble.Set(this.accKp, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>AccKi</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double AccKi {
			get => EditableDouble.Get(this.accKi);
			set => EditableDouble.Set(this.accKi, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>AccKd</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double AccKd {
			get => EditableDouble.Get(this.accKd);
			set => EditableDouble.Set(this.accKd, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>PitKp</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double PitKp {
			get => EditableDouble.Get(this.pitKp);
			set => EditableDouble.Set(this.pitKp, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>PitKi</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double PitKi {
			get => EditableDouble.Get(this.pitKi);
			set => EditableDouble.Set(this.pitKi, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>PitKd</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double PitKd {
			get => EditableDouble.Get(this.pitKd);
			set => EditableDouble.Set(this.pitKd, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RolKp</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double RolKp {
			get => EditableDouble.Get(this.rolKp);
			set => EditableDouble.Set(this.rolKp, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RolKi</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double RolKi {
			get => EditableDouble.Get(this.rolKi);
			set => EditableDouble.Set(this.rolKi, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RolKd</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double RolKd {
			get => EditableDouble.Get(this.rolKd);
			set => EditableDouble.Set(this.rolKd, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>YawKp</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double YawKp {
			get => EditableDouble.Get(this.yawKp);
			set => EditableDouble.Set(this.yawKp, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>YawKi</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double YawKi {
			get => EditableDouble.Get(this.yawKi);
			set => EditableDouble.Set(this.yawKi, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>YawKd</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double YawKd {
			get => EditableDouble.Get(this.yawKd);
			set => EditableDouble.Set(this.yawKd, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>YawLimit</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double YawLimit {
			get => EditableDouble.Get(this.yawLimit);
			set => EditableDouble.Set(this.yawLimit, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>RollLimit</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double RollLimit {
			get => EditableDouble.Get(this.rollLimit);
			set => EditableDouble.Set(this.rollLimit, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>PitchUpLimit</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double PitchUpLimit {
			get => EditableDouble.Get(this.pitchUpLimit);
			set => EditableDouble.Set(this.pitchUpLimit, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>PitchDownLimit</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public double PitchDownLimit {
			get => EditableDouble.Get(this.pitchDownLimit);
			set => EditableDouble.Set(this.pitchDownLimit, value);
		}
	}
}
