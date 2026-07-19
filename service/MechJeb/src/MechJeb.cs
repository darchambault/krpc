using System;
using System.Collections.Generic;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

using UnityEngine;

namespace KRPC.MechJeb {
	/// <summary>
	/// This service provides functionality to interact with <a href="https://github.com/MuMech/MechJeb2">MechJeb 2</a>.
	/// </summary>
	[KRPCService(Id = 8, GameScene = Service.GameScene.Flight)]
	public static class MechJeb {
		internal const string MechJebType = "MuMech.MechJebCore";

		internal static readonly List<string> errors = new List<string>();

		private static bool discoveryAttempted;
		private static bool assemblyAvailable;
		private static bool typesLoaded;
		private static bool instanceDiagnosticReported;
		private static Type type;
		private static FieldInfo vesselStateField;
		private static MethodInfo getComputerModule;

		internal static readonly VesselState vesselState = new VesselState();
		private static readonly Dictionary<string, Module> modules = new Dictionary<string, Module>();

		internal static bool InitTypes() {
			if(discoveryAttempted)
				return typesLoaded;

			discoveryAttempted = true;
			errors.Clear();
			try {
				// Scan the project assembly for MechJeb 2 reflection classes
				Dictionary<string, Type> mechjebTypes = new Dictionary<string, Type>();
				foreach(Type t in Assembly.GetExecutingAssembly().GetTypes()) {
					FieldInfo mechjebTypeField = t.GetField("MechJebType", BindingFlags.NonPublic | BindingFlags.Static);
					if(mechjebTypeField != null) {
						string mechjebType = (string)mechjebTypeField.GetValue(null);
						MechJebLogger.Debug("Found class " + t.Name + " wanting to use " + mechjebType);
						mechjebTypes.Add(mechjebType, t);
					}
				}

				// Scan all assemblies to match kRPC classes to MechJeb 2
				AssemblyLoader.loadedAssemblies.TypeOperation(mechjebType => {
					if(mechjebType.FullName == MechJebType)
						assemblyAvailable = true;
					if(mechjebTypes.TryGetValue(mechjebType.FullName, out Type internalType)) {
						mechjebTypes.Remove(mechjebType.FullName);
						try {
							MechJebLogger.Debug("Loading class " + internalType.Name + " using " + mechjebType.FullName);
							internalType.GetMethod("InitType", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { mechjebType });
						}
						catch(Exception ex) {
							string error = "Cannot load class " + internalType.Name;
							MechJebLogger.Error(error, ex);
							errors.Add(error);
						}
					}
				});

				// A missing optional dependency is normal. Only report missing wrapper types when
				// MechJeb itself is installed and its reflection contract is incomplete.
				if(assemblyAvailable) {
					foreach(KeyValuePair<string, Type> p in mechjebTypes) {
						string error = "Cannot load class " + p.Value.Name;
						MechJebLogger.Error(error + " because " + p.Key + " was not found");
						errors.Add(error);
					}
				}

				typesLoaded = assemblyAvailable && type != null && errors.Count == 0;
			}
			catch(Exception ex) {
				MechJebLogger.Error("Type discovery failed", ex);
				errors.Clear();
				errors.Add("kRPC.MechJeb failed to initialize: " + ex.Message);
				type = null;
				typesLoaded = false;
			}

			return typesLoaded;
		}

		internal static void InitType(Type t) {
			type = t;
			vesselStateField = t.GetCheckedField("VesselState");
			getComputerModule = t.GetCheckedMethod("GetComputerModule", new Type[] { typeof(string) });

			// MechJeb found, create module instances
			modules["AirplaneAutopilot"] = new AirplaneAutopilot();
			modules["AscentSettings"] = new AscentAutopilot();
			modules["DockingAutopilot"] = new DockingAutopilot();
			modules["LandingAutopilot"] = new LandingAutopilot();
			modules["RendezvousAutopilot"] = new RendezvousAutopilot();

			modules["ManeuverPlanner"] = new ManeuverPlanner();
			modules["SmartASS"] = new SmartASS();
			modules["SmartRCS"] = new SmartRCS();
			modules["Translatron"] = new Translatron();

			modules["DeployableAntennaController"] = new DeployableController();
			modules["NodeExecutor"] = new NodeExecutor();
			modules["RCSController"] = new RCSController();
			modules["StagingController"] = new StagingController();
			modules["SolarPanelController"] = new DeployableController();
			modules["TargetController"] = new TargetController();
			modules["ThrustController"] = new ThrustController();
		}

		internal static bool InitInstance() {
			ResetInstance();
			if(!typesLoaded || FlightGlobals.ActiveVessel == null)
				return false;

			bool modulesReady = true;
			try {
				MasterInstance = FlightGlobals.ActiveVessel.GetMasterMechJeb();
				if(MasterInstance == null)
					return false;

				vesselState.InitInstance(vesselStateField.GetInstanceValue(MasterInstance));

				// Set module instances to MechJeb objects
				foreach(KeyValuePair<string, Module> p in modules) {
					string error = "Cannot initialize class " + p.Value.GetType().Name + " with " + p.Key;
					try {
						object moduleInstance = GetComputerModule(p.Key);
						if(moduleInstance != null)
							p.Value.InitInstance(moduleInstance);
						else {
							RecordInstanceError(error);
							modulesReady = false;
						}
					}
					catch(Exception ex) {
						RecordInstanceError(error, ex);
						modulesReady = false;
					}
				}

				APIReady = modulesReady;
			}
			catch(Exception ex) {
				RecordInstanceError("kRPC.MechJeb failed to initialize: " + ex.Message, ex);
			}

			return APIReady;
		}

		internal static void ResetInstance(bool resetDiagnostic = false) {
			APIReady = false;
			MasterInstance = null;
			if(resetDiagnostic) {
				instanceDiagnosticReported = false;
				errors.Clear();
			}
		}

		internal static void ShowErrors(bool instanceDiagnostic = false) {
			if(errors.Count != 0) {
				PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "MechJebChecker", "kRPC.MechJeb may not work properly", string.Join("\n", errors.ToArray()), "OK", false, HighLogic.UISkin);
				errors.Clear();
				if(instanceDiagnostic)
					instanceDiagnosticReported = true;
			}
		}

		internal static object GetComputerModule(string moduleType) {
			return getComputerModule.Invoke(MasterInstance, new object[] { "MechJebModule" + moduleType });
		}

		private static void RecordInstanceError(string message, Exception exception = null) {
			if(instanceDiagnosticReported)
				return;

			if(exception == null)
				MechJebLogger.Error(message);
			else
				MechJebLogger.Error(message, exception);
			errors.Add(message);
		}

		internal static PartModule MasterInstance { get; private set; }

		internal static bool TypesLoaded => typesLoaded;

		/// <summary>
		/// Whether MechJeb 2 is installed.
		/// </summary>
		[KRPCProperty(GameScene = Service.GameScene.All)]
		public static bool Available {
			get {
				if(!discoveryAttempted)
					InitTypes();
				return assemblyAvailable;
			}
		}

		/// <summary>
		/// Whether the MechJeb API is initialized for the active vessel.
		/// </summary>
		[KRPCProperty]
		public static bool APIReady { get; private set; }

		// AUTOPILOTS

		/// <summary>
		/// Gets the value of <c>AirplaneAutopilot</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static AirplaneAutopilot AirplaneAutopilot => (AirplaneAutopilot)modules["AirplaneAutopilot"];

		/// <summary>
		/// Gets the value of <c>AscentAutopilot</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static AscentAutopilot AscentAutopilot => (AscentAutopilot)modules["AscentSettings"];

		/// <summary>
		/// Gets the value of <c>DockingAutopilot</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static DockingAutopilot DockingAutopilot => (DockingAutopilot)modules["DockingAutopilot"];

		/// <summary>
		/// Gets the value of <c>LandingAutopilot</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static LandingAutopilot LandingAutopilot => (LandingAutopilot)modules["LandingAutopilot"];

		/// <summary>
		/// Gets the value of <c>RendezvousAutopilot</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static RendezvousAutopilot RendezvousAutopilot => (RendezvousAutopilot)modules["RendezvousAutopilot"];

		// WINDOWS

		/// <summary>
		/// Gets the value of <c>ManeuverPlanner</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static ManeuverPlanner ManeuverPlanner => (ManeuverPlanner)modules["ManeuverPlanner"];

		/// <summary>
		/// Gets the value of <c>SmartASS</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static SmartASS SmartASS => (SmartASS)modules["SmartASS"];

		/// <summary>
		/// Gets the value of <c>SmartRCS</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static SmartRCS SmartRCS => (SmartRCS)modules["SmartRCS"];

		/// <summary>
		/// Gets the value of <c>Translatron</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static Translatron Translatron => (Translatron)modules["Translatron"];

		// CONTROLLERS

		/// <summary>
		/// Gets the value of <c>AntennaController</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static DeployableController AntennaController => (DeployableController)modules["DeployableAntennaController"];

		/// <summary>
		/// Gets the value of <c>NodeExecutor</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static NodeExecutor NodeExecutor => (NodeExecutor)modules["NodeExecutor"];

		/// <summary>
		/// Gets the value of <c>RCSController</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static RCSController RCSController => (RCSController)modules["RCSController"];

		/// <summary>
		/// Gets the value of <c>StagingController</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static StagingController StagingController => (StagingController)modules["StagingController"];

		/// <summary>
		/// Gets the value of <c>SolarPanelController</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static DeployableController SolarPanelController => (DeployableController)modules["SolarPanelController"];

		/// <summary>
		/// Gets the value of <c>TargetController</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static TargetController TargetController => (TargetController)modules["TargetController"];

		/// <summary>
		/// Gets the value of <c>ThrustController</c> in MechJeb.
		/// </summary>
		[KRPCProperty]
		public static ThrustController ThrustController => (ThrustController)modules["ThrustController"];
	}

	/// <summary>
	/// General exception for errors in the service.
	/// </summary>
	[KRPCException(Service = "MechJeb")]
	public class MJServiceException : Exception {
		/// <summary>
		/// Initializes an exception with the specified error message.
		/// </summary>
		/// <param name="message">A description of the service error.</param>
		public MJServiceException(string message) : base(message) { }
	}
}
