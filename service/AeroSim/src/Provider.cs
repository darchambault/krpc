using KRPC.Service.Attributes;
using KSPAeroSim = KRPC.SpaceCenter.ExternalAPI.KSPAeroSim;

namespace KRPC.AeroSim
{
    /// <summary>
    /// Metadata describing one registered aerodynamic provider.
    /// Obtained by calling <see cref="AeroSim.Providers"/>.
    /// </summary>
    [KRPCClass (Service = "AeroSim")]
    public class Provider
    {
        internal Provider (KSPAeroSim.ProviderData data)
        {
            Id = data.Id;
            Version = data.Version;
            Priority = data.Priority;
            SupportsEditor = data.SupportsEditor;
        }

        /// <summary>
        /// The identifier of the provider, for example "stock".
        /// </summary>
        [KRPCProperty]
        public string Id { get; private set; }

        /// <summary>
        /// The version of the provider, as a string.
        /// </summary>
        [KRPCProperty]
        public string Version { get; private set; }

        /// <summary>
        /// The priority of the provider. Model creation automatically selects
        /// the compatible provider with the highest priority.
        /// </summary>
        [KRPCProperty]
        public int Priority { get; private set; }

        /// <summary>
        /// Whether the provider can also capture models of editor craft.
        /// Editor capture is not exposed by this service.
        /// </summary>
        [KRPCProperty]
        public bool SupportsEditor { get; private set; }
    }
}
