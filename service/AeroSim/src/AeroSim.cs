using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.AeroSim
{
    /// <summary>
    /// This service provides functionality to interact with the KSPAeroSim mod,
    /// which captures detached aerodynamic models of a vessel. A model is an
    /// immutable snapshot of the vessel component that would remain after
    /// applying staging: it can be evaluated at arbitrary hypothetical flight
    /// states without affecting the live vessel, and remains valid after the
    /// source vessel stages, unloads or is destroyed. On top of model
    /// evaluation, the service exposes pitch-trim analysis and 6-DOF reentry
    /// prediction sessions.
    /// </summary>
    [KRPCService (Id = 12, GameScene = GameScene.Flight)]
    public static class AeroSim
    {
        static IList<Provider> providers;

        /// <summary>
        /// Whether the KSPAeroSim mod is installed and its API version is
        /// compatible with this service.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.All)]
        public static bool Available {
            get { return KSPAeroSim.IsAvailable; }
        }

        /// <summary>
        /// The version of the KSPAeroSim API, as a string. For example "0.2.0".
        /// </summary>
        [KRPCProperty]
        public static string ApiVersion {
            get {
                KSPAeroSim.CheckAvailable ();
                return KSPAeroSim.ApiVersion.ToString ();
            }
        }

        /// <summary>
        /// The registered aerodynamic providers, in decreasing order of
        /// priority. Model creation automatically selects the compatible
        /// provider with the highest priority, unless a provider identifier
        /// is given explicitly.
        /// </summary>
        [KRPCProperty]
        public static IList<Provider> Providers {
            get {
                KSPAeroSim.CheckAvailable ();
                if (providers == null) {
                    var result = new List<Provider> ();
                    foreach (var data in KSPAeroSim.GetProviders ())
                        result.Add (new Provider (data));
                    providers = result;
                }
                return providers;
            }
        }

        /// <summary>
        /// Capture an aerodynamic model of the given vessel after applying
        /// staging through <paramref name="afterStage"/>.
        /// </summary>
        /// <param name="vessel">The vessel to capture.</param>
        /// <param name="afterStage">The KSP stage number after which the
        /// vessel is modeled. Pass the vessel's current stage
        /// (<see cref="SpaceCenter.Services.Control.CurrentStage"/>) to
        /// capture the vessel as it currently is, or a lower stage number to
        /// model the component that would remain after activating the
        /// intervening stages.</param>
        /// <param name="retainedPart">A part identifying which projected
        /// component to model. When there is no retained part, the component
        /// containing the vessel's reference transform is modeled.</param>
        /// <param name="providerId">The identifier of the aerodynamic
        /// provider to use. When empty, the compatible provider with the
        /// highest priority is selected.</param>
        [KRPCProcedure]
        public static Model CreateModel (
            SpaceCenter.Services.Vessel vessel, int afterStage,
            [KRPCNullable] SpaceCenter.Services.Parts.Part retainedPart = null,
            string providerId = "")
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            return new Model (KSPAeroSim.CreateModel (
                vessel.InternalVessel, afterStage,
                ReferenceEquals (retainedPart, null) ? null : retainedPart.InternalPart,
                string.IsNullOrEmpty (providerId) ? null : providerId));
        }

        /// <summary>
        /// Capture aerodynamic models of every component the given vessel
        /// projects to after applying staging through
        /// <paramref name="afterStage"/>. The component containing the
        /// vessel's reference transform is returned first.
        /// </summary>
        /// <param name="vessel">The vessel to capture.</param>
        /// <param name="afterStage">The KSP stage number after which the
        /// vessel is modeled, as for <see cref="CreateModel"/>.</param>
        /// <param name="providerId">The identifier of the aerodynamic
        /// provider to use. When empty, the compatible provider with the
        /// highest priority is selected.</param>
        [KRPCProcedure]
        public static IList<Model> CreateModels (
            SpaceCenter.Services.Vessel vessel, int afterStage,
            string providerId = "")
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            var models = new List<Model> ();
            foreach (var model in KSPAeroSim.CreateModels (
                         vessel.InternalVessel, afterStage,
                         string.IsNullOrEmpty (providerId) ? null : providerId))
                models.Add (new Model (model));
            return models;
        }

        /// <summary>
        /// Decompose an aerodynamic force into drag and lift relative to an
        /// air-relative velocity. Both vectors must be expressed in the same
        /// reference frame; this is a purely geometric operation and no frame
        /// conversion is performed.
        /// </summary>
        /// <param name="force">The aerodynamic force, in newtons.</param>
        /// <param name="airRelativeVelocity">The air-relative velocity the
        /// force was evaluated at.</param>
        /// <returns>A triple of the drag (in newtons), the lift (in newtons)
        /// and the lift-to-drag ratio. The ratio is NaN when the drag is
        /// approximately zero</returns>
        [KRPCProcedure]
        public static Tuple3 DecomposeForce (Tuple3 force, Tuple3 airRelativeVelocity)
        {
            var metrics = KSPAeroSim.DecomposeForce (
                force.ToVector (), airRelativeVelocity.ToVector ());
            return new Tuple3 (metrics.Drag, metrics.Lift, metrics.LiftToDrag);
        }
    }
}
