using System;
using KRPC.Service.Attributes;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;

namespace KRPC.AeroSim
{
    /// <summary>
    /// An incremental pitch-trim analysis, created by
    /// <see cref="Model.CreatePitchTrimSession"/>. Each step performs at most
    /// one aerodynamic model evaluation; a full analysis typically takes on
    /// the order of 40 to 120 evaluations.
    /// </summary>
    [KRPCClass (Service = "AeroSim")]
    public class TrimSession
    {
        object session;
        TrimResult result;

        internal TrimSession (object session)
        {
            this.session = session;
        }

        object Handle {
            get {
                if (session == null)
                    throw new InvalidOperationException ("The session has been released.");
                return session;
            }
        }

        /// <summary>
        /// Advance the analysis by up to <paramref name="count"/> steps, each
        /// performing at most one aerodynamic model evaluation. Returns
        /// whether the analysis is complete. The steps run synchronously
        /// within the game's fixed update, so keep the batch size moderate;
        /// a count of 50 completes a typical analysis in a few calls.
        /// </summary>
        /// <param name="count">The maximum number of steps to advance by.</param>
        [KRPCMethod]
        public bool Step (int count = 1)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException (nameof (count));
            var handle = Handle;
            for (int i = 0; i < count; ++i) {
                if (KSPAeroSim.TrimStep (handle))
                    return true;
            }
            return KSPAeroSim.TrimIsComplete (handle);
        }

        /// <summary>
        /// Whether the analysis is complete and <see cref="Result"/> is
        /// available.
        /// </summary>
        [KRPCProperty]
        public bool IsComplete {
            get { return KSPAeroSim.TrimIsComplete (Handle); }
        }

        /// <summary>
        /// The number of aerodynamic model evaluations performed so far.
        /// </summary>
        [KRPCProperty]
        public int EvaluationCount {
            get { return KSPAeroSim.TrimEvaluationCount (Handle); }
        }

        /// <summary>
        /// The result of the analysis, or null until it is complete.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public TrimResult Result {
            get {
                if (result == null) {
                    var handle = KSPAeroSim.TrimResult (Handle);
                    if (handle != null)
                        result = new TrimResult (handle);
                }
                return result;
            }
        }

        /// <summary>
        /// Release the session so the server can reclaim its memory. All
        /// subsequent use of this session throws an exception. A result
        /// obtained before the release remains valid.
        /// </summary>
        [KRPCMethod]
        public void Release ()
        {
            session = null;
        }
    }
}
