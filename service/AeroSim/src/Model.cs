using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;
using TupleT3 = System.Tuple<System.Tuple<double, double, double>, System.Tuple<double, double, double>>;

namespace KRPC.AeroSim
{
    /// <summary>
    /// An immutable aerodynamic snapshot of one projected vessel component,
    /// captured by <see cref="AeroSim.CreateModel"/> or
    /// <see cref="AeroSim.CreateModels"/>. The model holds no references to
    /// the source vessel: it remains valid after the vessel stages, unloads
    /// or is destroyed, but it does not track later changes to the vessel.
    /// Create a new model to capture changed state.
    /// </summary>
    [KRPCClass (Service = "AeroSim")]
    public class Model
    {
        object model;

        internal Model (object model)
        {
            this.model = model;
        }

        object Handle {
            get {
                if (model == null)
                    throw new InvalidOperationException ("The model has been released.");
                return model;
            }
        }

        /// <summary>
        /// The identifier of the aerodynamic provider that captured this model.
        /// </summary>
        [KRPCProperty]
        public string ProviderId {
            get { return KSPAeroSim.ModelProviderId (Handle); }
        }

        /// <summary>
        /// The KSP stage number after which the vessel was modeled.
        /// </summary>
        [KRPCProperty]
        public int AfterStage {
            get { return KSPAeroSim.ModelAfterStage (Handle); }
        }

        /// <summary>
        /// Whether this model is of the component containing the source
        /// vessel's reference transform.
        /// </summary>
        [KRPCProperty]
        public bool IsReferenceComponent {
            get { return KSPAeroSim.ModelIsReferenceComponent (Handle); }
        }

        /// <summary>
        /// The KSP flight identifiers of the source parts included in this
        /// model.
        /// </summary>
        [KRPCProperty]
        public IList<uint> SourcePartFlightIds {
            get { return KSPAeroSim.ModelSourcePartFlightIds (Handle); }
        }

        /// <summary>
        /// The mass of the modeled component, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double Mass {
            get { return KSPAeroSim.ModelMass (Handle); }
        }

        /// <summary>
        /// The center of mass of the modeled component at capture time, in the
        /// given reference frame.
        /// </summary>
        /// <param name="referenceFrame">The reference frame to return the
        /// position in.</param>
        [KRPCMethod]
        public Tuple3 CapturedCenterOfMass (SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (
                KSPAeroSim.ModelCenterOfMass (Handle)).ToTuple ();
        }

        /// <summary>
        /// The orientation of the source vessel's reference transform at
        /// capture time, in the given reference frame, in the same form as
        /// <see cref="SpaceCenter.Services.Vessel.Rotation"/>.
        /// </summary>
        /// <param name="referenceFrame">The reference frame to return the
        /// rotation in.</param>
        [KRPCMethod]
        public Tuple4 CapturedReferenceRotation (SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.RotationFromWorldSpace (
                KSPAeroSim.ModelReferenceRotation (Handle)).ToTuple ();
        }

        /// <summary>
        /// Evaluate the aerodynamic force and torque acting on the modeled
        /// component at a hypothetical rigid-body state.
        /// </summary>
        /// <param name="body">The celestial body whose atmosphere the wrench
        /// is evaluated in.</param>
        /// <param name="referenceFrame">The reference frame that the position,
        /// velocity, rotation and angular velocity are in, and that the
        /// returned vectors are in. For future-state prediction,
        /// <see cref="SpaceCenter.Services.CelestialBody.NonRotatingReferenceFrame"/>
        /// is recommended so that the spatial state has unambiguous inertial
        /// semantics.</param>
        /// <param name="position">The position of the component's center of
        /// mass.</param>
        /// <param name="velocity">The velocity of the component's center of
        /// mass.</param>
        /// <param name="rotation">The orientation of the component, in the
        /// same form as <see cref="SpaceCenter.Services.Vessel.Rotation"/>.</param>
        /// <param name="angularVelocity">The angular velocity of the
        /// component. This adds the solid-body rotation term to each part's
        /// local airflow and the rigid-body angular drag the game applies,
        /// together giving the aerodynamic damping force and torque. Pass a
        /// zero vector to evaluate the static wrench relative to the
        /// reference frame.</param>
        /// <param name="ut">The universal time used for the atmospheric
        /// ephemeris. It selects the body/Sun geometry used for temperature
        /// and density, but does not change or propagate any of the state
        /// arguments.</param>
        /// <returns>A pair containing the aerodynamic force in newtons
        /// followed by the aerodynamic torque in newton-meters about the
        /// component's center of mass. Both are vectors in
        /// <paramref name="referenceFrame"/>.</returns>
        [KRPCMethod]
        public TupleT3 Evaluate (
            SpaceCenter.Services.CelestialBody body,
            SpaceCenter.Services.ReferenceFrame referenceFrame,
            Tuple3 position, Tuple3 velocity, Tuple4 rotation,
            Tuple3 angularVelocity, double ut)
        {
            CheckStateArguments (body, referenceFrame);
            var wrench = KSPAeroSim.Evaluate (
                Handle, body.InternalBody,
                referenceFrame.PositionToWorldSpace (position.ToVector ()),
                referenceFrame.VelocityToWorldSpace (position.ToVector (), velocity.ToVector ()),
                referenceFrame.RotationToWorldSpace (rotation.ToQuaternion ()),
                referenceFrame.AngularVelocityToWorldSpace (angularVelocity.ToVector ()),
                ut);
            return new TupleT3 (
                referenceFrame.DirectionFromWorldSpace (wrench.Force).ToTuple (),
                referenceFrame.DirectionFromWorldSpace (wrench.Torque).ToTuple ());
        }

        /// <summary>
        /// Create an incremental pitch-trim analysis of this model at one
        /// flight condition. The analysis samples the pitch-axis torque over
        /// attitudes from -30 to +30 degrees of pitch relative to the airflow
        /// (at zero body rate and zero bank), refines the torque roots, and
        /// selects the restoring root nearest zero. Advance it by calling
        /// <see cref="TrimSession.Step"/>.
        /// </summary>
        /// <param name="body">The celestial body whose atmosphere the
        /// condition is evaluated in.</param>
        /// <param name="referenceFrame">The reference frame that the position,
        /// velocity, rotation and angular velocity are in.</param>
        /// <param name="position">The position of the component's center of
        /// mass.</param>
        /// <param name="velocity">The velocity of the component's center of
        /// mass. The trim condition must have non-zero airspeed.</param>
        /// <param name="rotation">The orientation of the component. The
        /// analysis constructs its own attitudes relative to the airflow, so
        /// this only validates the state.</param>
        /// <param name="angularVelocity">The angular velocity of the
        /// component, used only to validate the state; the analysis evaluates
        /// at zero body rate.</param>
        /// <param name="ut">The universal time used for the atmospheric
        /// ephemeris.</param>
        [KRPCMethod]
        public TrimSession CreatePitchTrimSession (
            SpaceCenter.Services.CelestialBody body,
            SpaceCenter.Services.ReferenceFrame referenceFrame,
            Tuple3 position, Tuple3 velocity, Tuple4 rotation,
            Tuple3 angularVelocity, double ut)
        {
            CheckStateArguments (body, referenceFrame);
            return new TrimSession (KSPAeroSim.CreatePitchTrimSession (
                Handle, body.InternalBody,
                referenceFrame.PositionToWorldSpace (position.ToVector ()),
                referenceFrame.VelocityToWorldSpace (position.ToVector (), velocity.ToVector ()),
                referenceFrame.RotationToWorldSpace (rotation.ToQuaternion ()),
                referenceFrame.AngularVelocityToWorldSpace (angularVelocity.ToVector ()),
                ut));
        }

        /// <summary>
        /// Create a cooperative 6 degree-of-freedom reentry prediction that
        /// propagates this model from the given initial state until it
        /// reaches the stop altitude, the maximum time, or escapes the
        /// atmosphere. Advance it by calling
        /// <see cref="ReentryPrediction.Step"/>.
        /// </summary>
        /// <param name="body">The celestial body to predict the entry at.
        /// Must have an atmosphere.</param>
        /// <param name="referenceFrame">The reference frame that the initial
        /// position, velocity, rotation and angular velocity are in.</param>
        /// <param name="position">The initial position of the component's
        /// center of mass.</param>
        /// <param name="velocity">The initial velocity of the component's
        /// center of mass.</param>
        /// <param name="rotation">The initial orientation of the component,
        /// in the same form as
        /// <see cref="SpaceCenter.Services.Vessel.Rotation"/>.</param>
        /// <param name="angularVelocity">The initial angular velocity of the
        /// component, in radians per second.</param>
        /// <param name="ut">The universal time of the initial state.</param>
        /// <param name="inertiaTensor">The symmetric rigid-body inertia
        /// tensor about the center of mass, in the model's reference
        /// transform frame, in <math>kg.m^2</math>, as the six upper-triangular
        /// components <math>(I_{xx}, I_{xy}, I_{xz}, I_{yy}, I_{yz}, I_{zz})</math>.
        /// Must be positive definite.
        /// See <see cref="SpaceCenter.Services.Vessel.InertiaTensor"/>.</param>
        /// <param name="stopAltitude">The altitude, in meters above sea
        /// level, at which the prediction stops. Must be below the initial
        /// altitude.</param>
        /// <param name="atmosphericTimeStep">The integration time step below
        /// the top of the atmosphere, in seconds.</param>
        /// <param name="vacuumTimeStep">The integration time step above the
        /// atmosphere, in seconds.</param>
        /// <param name="recordInterval">The interval between recorded
        /// samples, in seconds of simulated time.</param>
        /// <param name="maximumTime">The maximum simulated time, in seconds,
        /// after which the prediction stops.</param>
        [KRPCMethod]
        public ReentryPrediction CreateReentryPrediction (
            SpaceCenter.Services.CelestialBody body,
            SpaceCenter.Services.ReferenceFrame referenceFrame,
            Tuple3 position, Tuple3 velocity, Tuple4 rotation,
            Tuple3 angularVelocity, double ut, IList<double> inertiaTensor,
            double stopAltitude = 0, double atmosphericTimeStep = 0.1,
            double vacuumTimeStep = 2, double recordInterval = 0.5,
            double maximumTime = 2000)
        {
            CheckStateArguments (body, referenceFrame);
            if (inertiaTensor == null)
                throw new ArgumentNullException (nameof (inertiaTensor));
            if (inertiaTensor.Count != 6)
                throw new ArgumentException (
                    "The inertia tensor must have exactly 6 elements " +
                    "(xx, xy, xz, yy, yz, zz).", nameof (inertiaTensor));
            return new ReentryPrediction (KSPAeroSim.CreateReentryPrediction (
                Handle, body.InternalBody,
                referenceFrame.PositionToWorldSpace (position.ToVector ()),
                referenceFrame.VelocityToWorldSpace (position.ToVector (), velocity.ToVector ()),
                referenceFrame.RotationToWorldSpace (rotation.ToQuaternion ()),
                referenceFrame.AngularVelocityToWorldSpace (angularVelocity.ToVector ()),
                ut,
                inertiaTensor [0], inertiaTensor [1], inertiaTensor [2],
                inertiaTensor [3], inertiaTensor [4], inertiaTensor [5],
                stopAltitude, atmosphericTimeStep, vacuumTimeStep,
                recordInterval, maximumTime));
        }

        /// <summary>
        /// Release the captured snapshot so the server can reclaim its
        /// memory. All subsequent use of this model throws an exception.
        /// Sessions created from the model are unaffected.
        /// </summary>
        [KRPCMethod]
        public void Release ()
        {
            model = null;
        }

        static void CheckStateArguments (
            SpaceCenter.Services.CelestialBody body,
            SpaceCenter.Services.ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
        }
    }
}
