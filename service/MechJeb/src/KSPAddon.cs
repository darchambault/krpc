using UnityEngine;

namespace KRPC.MechJeb {
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class InitTypes : MonoBehaviour {
		/// <summary>
		/// Invokes MechJeb's <c>Start</c> operation.
		/// </summary>
		public void Start() {
			MechJebLogger.Info("Loading MechJeb types");
			bool typesLoaded = MechJeb.InitTypes();
			if(!MechJeb.Available)
				MechJebLogger.Info("MechJeb is not installed");
			else if(typesLoaded)
				MechJebLogger.Info("MechJeb reflection API loaded");
			else
				MechJebLogger.Error("MechJeb is installed but its reflection API could not be loaded");
			MechJeb.ShowErrors();
		}
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	internal class InitInstance : MonoBehaviour {
		private const float InitializationRetryDelay = 1f;

		private Vessel activeVessel;
		private float nextInitializationAttempt;
		private bool initializationLogged;

		/// <summary>
		/// Invokes MechJeb's <c>LateUpdate</c> operation.
		/// </summary>
		public void LateUpdate() {
			if(!MechJeb.TypesLoaded)
				return;

			Vessel currentVessel = FlightGlobals.ActiveVessel;
			if(currentVessel == null) {
				if(this.activeVessel != null)
					MechJeb.ResetInstance(true);
				this.activeVessel = null;
				this.initializationLogged = false;
				return;
			}

			// Refresh MechJeb instance when focus changes, a flight is reverted to launch,
			// or the cached MasterMechJeb PartModule has been destroyed (e.g. quicksave reload).
			if(this.activeVessel != currentVessel) {
				this.activeVessel = currentVessel;
				MechJeb.ResetInstance(true);
				this.nextInitializationAttempt = 0f;
				this.initializationLogged = false;
			}
			else if(MechJeb.APIReady && MechJeb.MasterInstance == null) {
				MechJeb.ResetInstance(true);
				this.nextInitializationAttempt = 0f;
				this.initializationLogged = false;
			}

			// MechJeb may add its master module after the flight scene becomes active. Retry
			// quietly until it is ready, without replacing the permanent wrapper objects.
			if(!MechJeb.APIReady && Time.realtimeSinceStartup >= this.nextInitializationAttempt) {
				this.nextInitializationAttempt = Time.realtimeSinceStartup + InitializationRetryDelay;
				if(!this.initializationLogged) {
					MechJebLogger.Info("Initializing MechJeb for the active vessel");
					this.initializationLogged = true;
				}
				if(MechJeb.InitInstance())
					MechJebLogger.Info("MechJeb API is ready");
				MechJeb.ShowErrors(true);
			}
		}
	}
}
