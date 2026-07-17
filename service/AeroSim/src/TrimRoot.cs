using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.AeroSim
{
    /// <summary>
    /// One refined root of the pitch-axis torque curve from a pitch-trim
    /// analysis, and the aerodynamic state at that root.
    /// </summary>
    [KRPCClass (Service = "AeroSim")]
    public class TrimRoot
    {
        readonly KSPAeroSim.TrimRootData data;

        internal TrimRoot (KSPAeroSim.TrimRootData data)
        {
            this.data = data;
        }

        /// <summary>
        /// The pitch angle of the root, in degrees relative to the airflow.
        /// </summary>
        [KRPCProperty]
        public double Angle {
            get { return data.Angle; }
        }

        /// <summary>
        /// The slope of the pitch-axis torque at the root, in newton-meters
        /// per degree. Negative for a restoring root.
        /// </summary>
        [KRPCProperty]
        public double Stiffness {
            get { return data.Stiffness; }
        }

        /// <summary>
        /// The static stability of the root.
        /// </summary>
        [KRPCProperty]
        public TrimStability Stability {
            get { return (TrimStability)data.Stability; }
        }

        /// <summary>
        /// The aerodynamic force at the root, in newtons, in the given
        /// reference frame.
        /// </summary>
        /// <param name="referenceFrame">The reference frame to return the
        /// force in.</param>
        [KRPCMethod]
        public Tuple3 Force (SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (data.Wrench.Force).ToTuple ();
        }

        /// <summary>
        /// The aerodynamic torque at the root, in newton-meters about the
        /// component's center of mass, in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame">The reference frame to return the
        /// torque in.</param>
        [KRPCMethod]
        public Tuple3 Torque (SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (data.Wrench.Torque).ToTuple ();
        }

        /// <summary>
        /// The drag component of the force at the root, in newtons.
        /// </summary>
        [KRPCProperty]
        public double Drag {
            get { return data.Metrics.Drag; }
        }

        /// <summary>
        /// The lift component of the force at the root, in newtons.
        /// </summary>
        [KRPCProperty]
        public double Lift {
            get { return data.Metrics.Lift; }
        }

        /// <summary>
        /// The lift-to-drag ratio at the root. NaN when the drag is
        /// approximately zero.
        /// </summary>
        [KRPCProperty]
        public double LiftToDrag {
            get { return data.Metrics.LiftToDrag; }
        }
    }
}
