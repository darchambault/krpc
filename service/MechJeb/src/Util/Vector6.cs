using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Util {
	/// <summary>
	/// Provides access to MechJeb's <c>Vector6</c> functionality.
	/// </summary>
	public class Vector6 {
		/// <summary>
		/// Specifies values for <c>Direction</c>.
		/// </summary>
		[KRPCEnum(Service = "MechJeb")]
		public enum Direction {
			/// <summary>
			/// Selects <c>Forward</c>.
			/// </summary>
			Forward,
			/// <summary>
			/// Selects <c>Back</c>.
			/// </summary>
			Back,
			/// <summary>
			/// Selects <c>Up</c>.
			/// </summary>
			Up,
			/// <summary>
			/// Selects <c>Down</c>.
			/// </summary>
			Down,
			/// <summary>
			/// Selects <c>Right</c>.
			/// </summary>
			Right,
			/// <summary>
			/// Selects <c>Left</c>.
			/// </summary>
			Left
		}
	}
}
