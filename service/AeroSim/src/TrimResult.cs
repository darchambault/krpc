using System.Collections.Generic;
using KRPC.Service.Attributes;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;

namespace KRPC.AeroSim
{
    /// <summary>
    /// The static stability of the aerodynamic torque at a trim root.
    /// </summary>
    [KRPCEnum (Service = "AeroSim")]
    public enum TrimStability
    {
        /// <summary>
        /// The torque opposes displacement away from the root; the attitude
        /// is statically stable.
        /// </summary>
        Restoring,
        /// <summary>
        /// The torque slope at the root is approximately zero.
        /// </summary>
        Neutral,
        /// <summary>
        /// The torque grows with displacement away from the root; the
        /// attitude is statically unstable.
        /// </summary>
        Unstable
    }

    /// <summary>
    /// The completed result of a pitch-trim analysis, obtained from
    /// <see cref="TrimSession.Result"/>. The result is an immutable snapshot;
    /// it remains valid after the session is released.
    /// </summary>
    [KRPCClass (Service = "AeroSim")]
    public class TrimResult
    {
        internal TrimResult (object result)
        {
            SampleAngles = KSPAeroSim.TrimResultSampleAngles (result);
            SampleTorques = KSPAeroSim.TrimResultSampleTorques (result);
            var roots = new List<TrimRoot> ();
            foreach (var root in KSPAeroSim.TrimResultRoots (result))
                roots.Add (new TrimRoot (root));
            Roots = roots;
            var selected = KSPAeroSim.TrimResultSelectedRootIndex (result);
            SelectedRoot = selected >= 0 ? roots [selected] : null;
        }

        /// <summary>
        /// The pitch angles the torque curve was sampled at, in degrees
        /// relative to the airflow.
        /// </summary>
        [KRPCProperty]
        public IList<double> SampleAngles { get; private set; }

        /// <summary>
        /// The signed pitch-axis torque at each sampled angle, in
        /// newton-meters. Parallel to <see cref="SampleAngles"/>.
        /// </summary>
        [KRPCProperty]
        public IList<double> SampleTorques { get; private set; }

        /// <summary>
        /// All refined roots of the torque curve.
        /// </summary>
        [KRPCProperty]
        public IList<TrimRoot> Roots { get; private set; }

        /// <summary>
        /// The selected trim root: the restoring root nearest zero pitch,
        /// or null when there is no restoring root.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public TrimRoot SelectedRoot { get; private set; }

        /// <summary>
        /// Whether the analysis found a restoring (statically stable) root.
        /// </summary>
        [KRPCProperty]
        public bool HasStableRoot {
            get { return SelectedRoot != null; }
        }
    }
}
