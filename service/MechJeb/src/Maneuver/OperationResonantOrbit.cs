using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Resonant orbit is useful for placing satellites to a constellation. This mode should be used starting from a orbit in the desired orbital plane. Important parameter to this mode is the desired orbital ratio, which is the ratio between period of your current orbit and the new orbit.
	/// To deploy satellites, set the denominator to number of satellites you want to have in the constellation. Setting the nominator to one less than denominator is the most efficient, but not necessary the fastest. To successfully deploy all satellites, make sure the numbers are incommensurable.
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationResonantOrbit : TimedOperation {
		internal new const string MechJebType = "MuMech.OperationResonantOrbit";

		// Fields and methods
		private static FieldInfo resonanceNumeratorField;
		private static FieldInfo resonanceDenominatorField;
		private static FieldInfo timeSelector;

		// Instance objects
		private object resonanceNumerator;
		private object resonanceDenominator;

		internal static new void InitType(Type type) {
			resonanceNumeratorField = type.GetCheckedField("ResonanceNumerator");
			resonanceDenominatorField = type.GetCheckedField("ResonanceDenominator");
			timeSelector = GetTimeSelectorField(type);
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.resonanceNumerator = resonanceNumeratorField.GetInstanceValue(instance);
			this.resonanceDenominator = resonanceDenominatorField.GetInstanceValue(instance);
			this.InitTimeSelector(timeSelector);
		}

		/// <summary>
		/// Gets or sets the value of <c>ResonanceNumerator</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public int ResonanceNumerator {
			get => EditableInt.Get(this.resonanceNumerator);
			set => EditableInt.Set(this.resonanceNumerator, value);
		}

		/// <summary>
		/// Gets or sets the value of <c>ResonanceDenominator</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public int ResonanceDenominator {
			get => EditableInt.Get(this.resonanceDenominator);
			set => EditableInt.Set(this.resonanceDenominator, value);
		}
	}
}
