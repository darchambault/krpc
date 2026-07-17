using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;

namespace KRPC.AeroSim
{
    /// <summary>
    /// The condition that ended a reentry prediction.
    /// </summary>
    [KRPCEnum (Service = "AeroSim")]
    public enum ReentryPredictionTermination
    {
        /// <summary>
        /// The propagated state descended to the stop altitude.
        /// </summary>
        StopAltitudeReached,
        /// <summary>
        /// The maximum simulated time was reached.
        /// </summary>
        MaximumTimeReached,
        /// <summary>
        /// The propagated state is escaping the body.
        /// </summary>
        Escaping
    }

    /// <summary>
    /// The completed result of a reentry prediction, obtained from
    /// <see cref="ReentryPrediction.Result"/>.
    /// </summary>
    [KRPCClass (Service = "AeroSim")]
    public class ReentryPredictionResult
    {
        object result;

        internal ReentryPredictionResult (object result)
        {
            this.result = result;
            Termination = (ReentryPredictionTermination)KSPAeroSim.ResultTermination (result);
            Latitude = KSPAeroSim.ResultLatitude (result);
            Longitude = KSPAeroSim.ResultLongitude (result);
            SurfaceDownrange = KSPAeroSim.ResultSurfaceDownrange (result);
            PeakDynamicPressure = KSPAeroSim.ResultPeakDynamicPressure (result);
            MaximumAngleOfAttack = KSPAeroSim.ResultMaximumAngleOfAttack (result);
            MaximumBodyRate = KSPAeroSim.ResultMaximumBodyRate (result);
            EvaluationCount = KSPAeroSim.ResultEvaluationCount (result);
            EquivalentAerodynamicEndpointCallCount =
                KSPAeroSim.ResultEquivalentCallCount (result);
            WallTime = KSPAeroSim.ResultWallTime (result);
            FinalSample = KSPAeroSim.ResultFinalSample (result);
        }

        object Handle {
            get {
                if (result == null)
                    throw new InvalidOperationException ("The result has been released.");
                return result;
            }
        }

        /// <summary>
        /// The condition that ended the prediction.
        /// </summary>
        [KRPCProperty]
        public ReentryPredictionTermination Termination { get; private set; }

        /// <summary>
        /// Whether the prediction descended to the stop altitude.
        /// </summary>
        [KRPCProperty]
        public bool ReachedStopAltitude {
            get { return Termination == ReentryPredictionTermination.StopAltitudeReached; }
        }

        /// <summary>
        /// The latitude of the final state, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Latitude { get; private set; }

        /// <summary>
        /// The longitude of the final state, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Longitude { get; private set; }

        /// <summary>
        /// The great-circle distance traveled over the rotating surface, in
        /// meters.
        /// </summary>
        [KRPCProperty]
        public double SurfaceDownrange { get; private set; }

        /// <summary>
        /// The peak dynamic pressure encountered, in Pascals.
        /// </summary>
        [KRPCProperty]
        public double PeakDynamicPressure { get; private set; }

        /// <summary>
        /// The maximum angle of attack encountered, in degrees.
        /// </summary>
        [KRPCProperty]
        public double MaximumAngleOfAttack { get; private set; }

        /// <summary>
        /// The maximum body rotation rate encountered, in radians per second.
        /// </summary>
        [KRPCProperty]
        public double MaximumBodyRate { get; private set; }

        /// <summary>
        /// The total number of aerodynamic model evaluations performed.
        /// </summary>
        [KRPCProperty]
        public int EvaluationCount { get; private set; }

        /// <summary>
        /// The number of evaluations performed below the top of the
        /// atmosphere. Each corresponds to one call a client-side predictor
        /// would have made to an aerodynamic simulation endpoint.
        /// </summary>
        [KRPCProperty]
        public int EquivalentAerodynamicEndpointCallCount { get; private set; }

        /// <summary>
        /// The real time from the creation of the prediction to its
        /// completion, in seconds, including time between calls to
        /// <see cref="ReentryPrediction.Step"/>.
        /// </summary>
        [KRPCProperty]
        public double WallTime { get; private set; }

        /// <summary>
        /// The final state as one flattened sample, in the same 18-value
        /// layout as <see cref="ReentryPrediction.GetSamples"/>.
        /// </summary>
        [KRPCProperty]
        public IList<double> FinalSample { get; private set; }

        /// <summary>
        /// The number of recorded samples.
        /// </summary>
        [KRPCProperty]
        public int SampleCount {
            get { return KSPAeroSim.ResultSampleCount (Handle); }
        }

        /// <summary>
        /// The recorded samples with indices <paramref name="start"/> to
        /// <paramref name="start"/> + <paramref name="count"/> - 1, in the
        /// same flat 18-value layout as
        /// <see cref="ReentryPrediction.GetSamples"/>.
        /// </summary>
        /// <param name="start">The index of the first sample to return.</param>
        /// <param name="count">The number of samples to return.</param>
        [KRPCMethod]
        public IList<double> GetSamples (int start, int count)
        {
            return KSPAeroSim.ResultGetSamples (Handle, start, count);
        }

        /// <summary>
        /// Release the sample history so the server can reclaim its memory.
        /// The scalar properties and <see cref="FinalSample"/> remain
        /// available; <see cref="SampleCount"/> and <see cref="GetSamples"/>
        /// throw after the release.
        /// </summary>
        [KRPCMethod]
        public void Release ()
        {
            result = null;
        }
    }
}
