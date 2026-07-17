using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;

namespace KRPC.AeroSim
{
    /// <summary>
    /// A cooperative 6 degree-of-freedom reentry prediction, created by
    /// <see cref="Model.CreateReentryPrediction"/>. The prediction integrates
    /// the model's free attitude with an RK4 scheme; each step advances one
    /// RK stage and performs at most one aerodynamic model evaluation. A full
    /// atmospheric entry can take tens of thousands of evaluations, so
    /// advance the prediction with large step batches.
    /// </summary>
    [KRPCClass (Service = "AeroSim")]
    public class ReentryPrediction
    {
        object session;
        ReentryPredictionResult result;

        internal ReentryPrediction (object session)
        {
            this.session = session;
        }

        object Handle {
            get {
                if (session == null)
                    throw new InvalidOperationException ("The prediction has been released.");
                return session;
            }
        }

        /// <summary>
        /// Advance the prediction by up to <paramref name="count"/> steps,
        /// each performing at most one aerodynamic model evaluation. Returns
        /// whether the prediction is complete. The steps run synchronously
        /// within the game's fixed update, so the batch size trades RPC
        /// overhead against the game stalling; counts in the hundreds are a
        /// reasonable starting point.
        /// </summary>
        /// <param name="count">The maximum number of steps to advance by.</param>
        [KRPCMethod]
        public bool Step (int count = 1)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException (nameof (count));
            var handle = Handle;
            for (int i = 0; i < count; ++i) {
                if (KSPAeroSim.PredictionStep (handle))
                    return true;
            }
            return KSPAeroSim.PredictionIsComplete (handle);
        }

        /// <summary>
        /// Whether the prediction is complete and <see cref="Result"/> is
        /// available.
        /// </summary>
        [KRPCProperty]
        public bool IsComplete {
            get { return KSPAeroSim.PredictionIsComplete (Handle); }
        }

        /// <summary>
        /// The number of aerodynamic model evaluations performed so far.
        /// </summary>
        [KRPCProperty]
        public int EvaluationCount {
            get { return KSPAeroSim.PredictionEvaluationCount (Handle); }
        }

        /// <summary>
        /// The number of evaluations performed below the top of the
        /// atmosphere so far. Each corresponds to one call a client-side
        /// predictor would have made to an aerodynamic simulation endpoint.
        /// </summary>
        [KRPCProperty]
        public int EquivalentAerodynamicEndpointCallCount {
            get { return KSPAeroSim.PredictionEquivalentCallCount (Handle); }
        }

        /// <summary>
        /// The simulated time since the initial state, in seconds.
        /// </summary>
        [KRPCProperty]
        public double ElapsedTime {
            get { return KSPAeroSim.PredictionElapsedTime (Handle); }
        }

        /// <summary>
        /// The current altitude of the propagated state, in meters above sea
        /// level.
        /// </summary>
        [KRPCProperty]
        public double CurrentAltitude {
            get { return KSPAeroSim.PredictionCurrentAltitude (Handle); }
        }

        /// <summary>
        /// The number of samples recorded so far.
        /// </summary>
        [KRPCProperty]
        public int SampleCount {
            get { return KSPAeroSim.PredictionSampleCount (Handle); }
        }

        /// <summary>
        /// The recorded samples with indices <paramref name="start"/> to
        /// <paramref name="start"/> + <paramref name="count"/> - 1, as one
        /// flat list of 18 values per sample: the universal time, the elapsed
        /// time (s), the altitude (m), the body-centered position (3 values,
        /// m, in the celestial body's non-rotating axes at the initial
        /// state), the velocity (3 values, m/s, in the same axes), the
        /// rotation (4 values, x y z w), the angular velocity (3 values,
        /// rad/s), the airspeed (m/s) and the angle of attack (degrees).
        /// Samples are historical states, so no reference frame conversion is
        /// applied; the values match the celestial body's
        /// <see cref="SpaceCenter.Services.CelestialBody.NonRotatingReferenceFrame"/>
        /// interpretation of the initial state.
        /// </summary>
        /// <param name="start">The index of the first sample to return.</param>
        /// <param name="count">The number of samples to return.</param>
        [KRPCMethod]
        public IList<double> GetSamples (int start, int count)
        {
            return KSPAeroSim.PredictionGetSamples (Handle, start, count);
        }

        /// <summary>
        /// The result of the prediction, or null until it is complete.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public ReentryPredictionResult Result {
            get {
                if (result == null) {
                    var handle = KSPAeroSim.PredictionResult (Handle);
                    if (handle != null)
                        result = new ReentryPredictionResult (handle);
                }
                return result;
            }
        }

        /// <summary>
        /// Release the prediction so the server can reclaim its memory. All
        /// subsequent use of this prediction throws an exception. A result
        /// obtained before the release remains valid.
        /// </summary>
        [KRPCMethod]
        public void Release ()
        {
            session = null;
        }
    }
}
