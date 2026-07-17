using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// Extension methods for rigidbodies.
    /// </summary>
    static class RigidbodyExtensions
    {
        /// <summary>
        /// The angular momentum of a part's rigidbody if it were rotating at the given
        /// world-space angular velocity. Uses the rigidbody's own (Unity,
        /// collider-derived) inertia tensor, which is what the engine's angular drag
        /// acts on. KSP's Unity physics uses tonne/kilonewton units, so the inertia
        /// tensor is in tonne*m^2 and the result is in kilonewton-meter-seconds.
        /// </summary>
        public static Vector3 PartAngularMomentum(Rigidbody rb, Vector3 angularVelocity)
        {
            var inertiaFrame = rb.rotation * rb.inertiaTensorRotation;
            var omegaLocal = Quaternion.Inverse(inertiaFrame) * angularVelocity;
            var inertia = rb.inertiaTensor;
            return inertiaFrame * new Vector3(
                inertia.x * omegaLocal.x, inertia.y * omegaLocal.y, inertia.z * omegaLocal.z);
        }
    }
}
