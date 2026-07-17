#pragma warning disable 1591
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.ExternalAPI
{
    /// <summary>
    /// Reflection-based wrapper around the public API of the KSPAeroSim mod.
    /// The mod assembly is resolved at load time and all members are bound as
    /// compiled expression-tree delegates, so this assembly has no compile-time
    /// dependency on the mod. Mod objects (models, sessions, results) cross
    /// this boundary as opaque <c>object</c> handles; all other values are
    /// KSP/Unity types or the plain data carriers declared in this class.
    /// </summary>
    public static class KSPAeroSim
    {
        /// <summary>An aerodynamic force and center-of-mass torque in SI units.</summary>
        public struct Wrench
        {
            public Wrench (Vector3d force, Vector3d torque)
            {
                Force = force;
                Torque = torque;
            }

            public Vector3d Force { get; private set; }
            public Vector3d Torque { get; private set; }
        }

        /// <summary>Drag/lift decomposition relative to an air-relative velocity.</summary>
        public struct ForceMetrics
        {
            public ForceMetrics (double drag, double lift, double liftToDrag)
            {
                Drag = drag;
                Lift = lift;
                LiftToDrag = liftToDrag;
            }

            public double Drag { get; private set; }
            public double Lift { get; private set; }
            public double LiftToDrag { get; private set; }
        }

        /// <summary>Metadata describing one registered aerodynamic provider.</summary>
        public struct ProviderData
        {
            public ProviderData (string id, string version, int priority, bool supportsEditor)
            {
                Id = id;
                Version = version;
                Priority = priority;
                SupportsEditor = supportsEditor;
            }

            public string Id { get; private set; }
            public string Version { get; private set; }
            public int Priority { get; private set; }
            public bool SupportsEditor { get; private set; }
        }

        /// <summary>One refined trim root and its local static stability.</summary>
        public struct TrimRootData
        {
            public TrimRootData (double angle, double stiffness, int stability, Wrench wrench, ForceMetrics metrics)
            {
                Angle = angle;
                Stiffness = stiffness;
                Stability = stability;
                Wrench = wrench;
                Metrics = metrics;
            }

            public double Angle { get; private set; }
            public double Stiffness { get; private set; }
            public int Stability { get; private set; }
            public Wrench Wrench { get; private set; }
            public ForceMetrics Metrics { get; private set; }
        }

        /// <summary>Number of doubles per flattened reentry prediction sample.</summary>
        public const int SampleStride = 18;

        public static bool IsAvailable { get; private set; }

        /// <summary>The mod's API version, or null when the mod is not loaded.</summary>
        public static Version ApiVersion { get; private set; }

        /// <summary>
        /// Throws an exception when the KSPAeroSim mod is not installed or its
        /// API version is unsupported.
        /// </summary>
        public static void CheckAvailable ()
        {
            if (!IsAvailable)
                throw new InvalidOperationException ("KSPAeroSim is not installed.");
        }

        public static void Load ()
        {
            IsAvailable = false;
            ApiVersion = null;
            var assembly = AssemblyLoader.loadedAssemblies.FirstOrDefault (
                a => a.assembly.GetName ().Name == "KSPAeroSim");
            if (assembly == null) {
                KRPC.Utils.Logger.WriteLine ("Load API: KSPAeroSim not found; skipping");
                return;
            }
            try {
                LoadApi (assembly.assembly);
                IsAvailable = true;
                KRPC.Utils.Logger.WriteLine ("Load API: Successfully loaded KSPAeroSim version " + ApiVersion);
            } catch (Exception exn) {
                Error ("Failed to load KSPAeroSim: " + exn.Message);
            }
        }

        public static IList<ProviderData> GetProviders ()
        {
            CheckAvailable ();
            var result = new List<ProviderData> ();
            foreach (var info in (IEnumerable)getProviders ())
                result.Add (new ProviderData (
                    providerId (info), providerVersion (info).ToString (),
                    providerPriority (info), providerSupportsEditor (info)));
            return result;
        }

        public static object CreateModel (Vessel vessel, int afterStage, Part retainedPart, string providerId)
        {
            CheckAvailable ();
            return createModel (vessel, afterStage, retainedPart, providerId);
        }

        public static IList<object> CreateModels (Vessel vessel, int afterStage, string providerId)
        {
            CheckAvailable ();
            return ((IEnumerable)createModels (vessel, afterStage, providerId)).Cast<object> ().ToList ();
        }

        public static string ModelProviderId (object model)
        {
            return modelProviderId (model);
        }

        public static int ModelAfterStage (object model)
        {
            return modelAfterStage (model);
        }

        public static bool ModelIsReferenceComponent (object model)
        {
            return modelIsReferenceComponent (model);
        }

        public static IList<uint> ModelSourcePartFlightIds (object model)
        {
            return ((IEnumerable)modelSourcePartFlightIds (model)).Cast<uint> ().ToList ();
        }

        public static double ModelMass (object model)
        {
            return modelMass (model);
        }

        public static Vector3d ModelCenterOfMass (object model)
        {
            return modelCenterOfMass (model);
        }

        public static QuaternionD ModelReferenceRotation (object model)
        {
            return modelReferenceRotation (model);
        }

        /// <summary>
        /// Evaluate the model's aerodynamic wrench at a hypothetical rigid-body
        /// state expressed in KSP world space. Force is in newtons; torque is
        /// in newton-meters about the component's center of mass.
        /// </summary>
        public static Wrench Evaluate (object model, CelestialBody body, Vector3d position, Vector3d velocity, QuaternionD rotation, Vector3d angularVelocity, double ut)
        {
            return evaluate (model, body, position, velocity, rotation, angularVelocity, ut);
        }

        public static ForceMetrics DecomposeForce (Vector3d force, Vector3d airRelativeVelocity)
        {
            CheckAvailable ();
            return decomposeForce (force, airRelativeVelocity);
        }

        public static object CreatePitchTrimSession (object model, CelestialBody body, Vector3d position, Vector3d velocity, QuaternionD rotation, Vector3d angularVelocity, double ut)
        {
            return createPitchTrimSession (model, body, position, velocity, rotation, angularVelocity, ut);
        }

        public static bool TrimStep (object session)
        {
            return trimStep (session);
        }

        public static bool TrimIsComplete (object session)
        {
            return trimIsComplete (session);
        }

        public static int TrimEvaluationCount (object session)
        {
            return trimEvaluationCount (session);
        }

        /// <summary>The trim result handle, or null while the session is incomplete.</summary>
        public static object TrimResult (object session)
        {
            return trimResult (session);
        }

        public static IList<double> TrimResultSampleAngles (object result)
        {
            var angles = new List<double> ();
            foreach (var sample in (IEnumerable)trimResultSamples (result))
                angles.Add (torqueSampleAngle (sample));
            return angles;
        }

        public static IList<double> TrimResultSampleTorques (object result)
        {
            var torques = new List<double> ();
            foreach (var sample in (IEnumerable)trimResultSamples (result))
                torques.Add (torqueSampleTorque (sample));
            return torques;
        }

        public static IList<TrimRootData> TrimResultRoots (object result)
        {
            var roots = new List<TrimRootData> ();
            foreach (var root in (IEnumerable)trimResultRoots (result))
                roots.Add (trimRoot (root));
            return roots;
        }

        /// <summary>
        /// Index of the selected (restoring) root in the roots list, or -1 when
        /// the result has no stable root.
        /// </summary>
        public static int TrimResultSelectedRootIndex (object result)
        {
            var selected = trimResultSelectedRoot (result);
            if (selected == null)
                return -1;
            var index = 0;
            foreach (var root in (IEnumerable)trimResultRoots (result)) {
                if (ReferenceEquals (root, selected))
                    return index;
                ++index;
            }
            return -1;
        }

        /// <summary>
        /// Create a cooperative 6-DOF reentry prediction session. The inertia
        /// tensor components are in kg.m^2 about the center of mass, in the
        /// model's reference-transform frame.
        /// </summary>
        public static object CreateReentryPrediction (
            object model, CelestialBody body, Vector3d position, Vector3d velocity,
            QuaternionD rotation, Vector3d angularVelocity, double ut,
            double inertiaXX, double inertiaXY, double inertiaXZ,
            double inertiaYY, double inertiaYZ, double inertiaZZ,
            double stopAltitude, double atmosphericTimeStep, double vacuumTimeStep,
            double recordInterval, double maximumTime)
        {
            var inertia = createInertia (
                inertiaXX, inertiaXY, inertiaXZ, inertiaYY, inertiaYZ, inertiaZZ);
            var parameters = createPredictionParameters (
                body, position, velocity, rotation, angularVelocity, ut, inertia,
                stopAltitude, atmosphericTimeStep, vacuumTimeStep, recordInterval,
                maximumTime);
            return createPredictionSession (model, parameters);
        }

        public static bool PredictionStep (object session)
        {
            return predictionStep (session);
        }

        public static bool PredictionIsComplete (object session)
        {
            return predictionIsComplete (session);
        }

        public static int PredictionEvaluationCount (object session)
        {
            return predictionEvaluationCount (session);
        }

        public static int PredictionEquivalentCallCount (object session)
        {
            return predictionEquivalentCallCount (session);
        }

        public static double PredictionElapsedTime (object session)
        {
            return predictionElapsedTime (session);
        }

        public static double PredictionCurrentAltitude (object session)
        {
            return predictionCurrentAltitude (session);
        }

        public static int PredictionSampleCount (object session)
        {
            return ((ICollection)predictionSamples (session)).Count;
        }

        public static IList<double> PredictionGetSamples (object session, int start, int count)
        {
            return FlattenSamples ((IList)predictionSamples (session), start, count);
        }

        /// <summary>The prediction result handle, or null while the session is incomplete.</summary>
        public static object PredictionResult (object session)
        {
            return predictionResult (session);
        }

        public static int ResultTermination (object result)
        {
            return resultTermination (result);
        }

        public static double ResultLatitude (object result)
        {
            return resultLatitude (result);
        }

        public static double ResultLongitude (object result)
        {
            return resultLongitude (result);
        }

        public static double ResultSurfaceDownrange (object result)
        {
            return resultSurfaceDownrange (result);
        }

        public static double ResultPeakDynamicPressure (object result)
        {
            return resultPeakDynamicPressure (result);
        }

        public static double ResultMaximumAngleOfAttack (object result)
        {
            return resultMaximumAngleOfAttack (result);
        }

        public static double ResultMaximumBodyRate (object result)
        {
            return resultMaximumBodyRate (result);
        }

        public static int ResultEvaluationCount (object result)
        {
            return resultEvaluationCount (result);
        }

        public static int ResultEquivalentCallCount (object result)
        {
            return resultEquivalentCallCount (result);
        }

        public static double ResultWallTime (object result)
        {
            return resultWallTime (result);
        }

        public static IList<double> ResultFinalSample (object result)
        {
            var flattened = new List<double> (SampleStride);
            AppendSample (flattened, resultFinalState (result));
            return flattened;
        }

        public static int ResultSampleCount (object result)
        {
            return ((ICollection)resultSamples (result)).Count;
        }

        public static IList<double> ResultGetSamples (object result, int start, int count)
        {
            return FlattenSamples ((IList)resultSamples (result), start, count);
        }

        static Func<object> getProviders;
        static Func<object, string> providerId;
        static Func<object, object> providerVersion;
        static Func<object, int> providerPriority;
        static Func<object, bool> providerSupportsEditor;
        static Func<Vessel, int, Part, string, object> createModel;
        static Func<Vessel, int, string, object> createModels;
        static Func<object, string> modelProviderId;
        static Func<object, int> modelAfterStage;
        static Func<object, bool> modelIsReferenceComponent;
        static Func<object, object> modelSourcePartFlightIds;
        static Func<object, double> modelMass;
        static Func<object, Vector3d> modelCenterOfMass;
        static Func<object, QuaternionD> modelReferenceRotation;
        static Func<object, CelestialBody, Vector3d, Vector3d, QuaternionD, Vector3d, double, Wrench> evaluate;
        static Func<Vector3d, Vector3d, ForceMetrics> decomposeForce;
        static Func<object, CelestialBody, Vector3d, Vector3d, QuaternionD, Vector3d, double, object> createPitchTrimSession;
        static Func<object, bool> trimStep;
        static Func<object, bool> trimIsComplete;
        static Func<object, int> trimEvaluationCount;
        static Func<object, object> trimResult;
        static Func<object, object> trimResultSamples;
        static Func<object, object> trimResultRoots;
        static Func<object, object> trimResultSelectedRoot;
        static Func<object, double> torqueSampleAngle;
        static Func<object, double> torqueSampleTorque;
        static Func<object, TrimRootData> trimRoot;
        static Func<double, double, double, double, double, double, object> createInertia;
        static Func<CelestialBody, Vector3d, Vector3d, QuaternionD, Vector3d, double, object, double, double, double, double, double, object> createPredictionParameters;
        static Func<object, object, object> createPredictionSession;
        static Func<object, bool> predictionStep;
        static Func<object, bool> predictionIsComplete;
        static Func<object, int> predictionEvaluationCount;
        static Func<object, int> predictionEquivalentCallCount;
        static Func<object, double> predictionElapsedTime;
        static Func<object, double> predictionCurrentAltitude;
        static Func<object, object> predictionSamples;
        static Func<object, object> predictionResult;
        static Func<object, int> resultTermination;
        static Func<object, object> resultFinalState;
        static Func<object, double> resultLatitude;
        static Func<object, double> resultLongitude;
        static Func<object, double> resultSurfaceDownrange;
        static Func<object, double> resultPeakDynamicPressure;
        static Func<object, double> resultMaximumAngleOfAttack;
        static Func<object, double> resultMaximumBodyRate;
        static Func<object, int> resultEvaluationCount;
        static Func<object, int> resultEquivalentCallCount;
        static Func<object, double> resultWallTime;
        static Func<object, object> resultSamples;
        static Func<object, double> sampleUniversalTime;
        static Func<object, double> sampleElapsedTime;
        static Func<object, double> sampleAltitude;
        static Func<object, Vector3d> samplePosition;
        static Func<object, Vector3d> sampleVelocity;
        static Func<object, QuaternionD> sampleRotation;
        static Func<object, Vector3d> sampleAngularVelocity;
        static Func<object, double> sampleAirSpeed;
        static Func<object, double> sampleAngleOfAttack;

        static void LoadApi (Assembly assembly)
        {
            var aerodynamics = GetApiType (assembly, "Aerodynamics");
            var providerInfo = GetApiType (assembly, "AerodynamicProviderInfo");
            var model = GetApiType (assembly, "AerodynamicModel");
            var state = GetApiType (assembly, "AerodynamicState");
            var wrench = GetApiType (assembly, "AerodynamicWrench");
            var analysis = GetApiType (assembly, "AerodynamicAnalysis");
            var forceMetrics = GetApiType (assembly, "AerodynamicForceMetrics");
            var torqueSample = GetApiType (assembly, "AerodynamicTorqueSample");
            var trimStability = GetApiType (assembly, "AerodynamicTrimStability");
            var trimSession = GetApiType (assembly, "AerodynamicTrimSession");
            var trimResultType = GetApiType (assembly, "AerodynamicTrimResult");
            var trimRootType = GetApiType (assembly, "AerodynamicTrimRoot");
            var reentryAuthority = GetApiType (assembly, "ReentryAuthorityAnalysis");
            var inertia = GetApiType (assembly, "RigidBodyInertia");
            var termination = GetApiType (assembly, "ReentryPredictionTermination");
            var predictionParameters = GetApiType (assembly, "ReentryPredictionParameters");
            var predictionSession = GetApiType (assembly, "ReentryPredictionSession");
            var predictionSample = GetApiType (assembly, "ReentryPredictionSample");
            var predictionResultType = GetApiType (assembly, "ReentryPredictionResult");

            ApiVersion = (Version)GetStaticProperty (aerodynamics, "ApiVersion").GetValue (null, null);
            if (ApiVersion.Major != 0 || ApiVersion.Minor != 2)
                throw new InvalidOperationException (
                    "unsupported API version " + ApiVersion + "; expected 0.2.x");

            CheckEnum (trimStability, "Restoring", "Neutral", "Unstable");
            CheckEnum (termination, "StopAltitudeReached", "MaximumTimeReached", "Escaping");

            var stateCtor = GetConstructor (state,
                typeof(CelestialBody), typeof(Vector3d), typeof(Vector3d),
                typeof(QuaternionD), typeof(Vector3d), typeof(double));

            getProviders = CompileStaticGetter<object> (aerodynamics, "Providers");
            providerId = CompileGetter<string> (providerInfo, "Id");
            providerVersion = CompileGetter<object> (providerInfo, "Version");
            providerPriority = CompileGetter<int> (providerInfo, "Priority");
            providerSupportsEditor = CompileGetter<bool> (providerInfo, "SupportsEditor");

            createModel = CompileStaticCall<Func<Vessel, int, Part, string, object>> (
                aerodynamics, "CreateModel",
                new[] { typeof(Vessel), typeof(int), typeof(Part), typeof(string) });
            createModels = CompileStaticCall<Func<Vessel, int, string, object>> (
                aerodynamics, "CreateModels",
                new[] { typeof(Vessel), typeof(int), typeof(string) });

            modelProviderId = CompileGetter<string> (model, "ProviderId");
            modelAfterStage = CompileGetter<int> (model, "AfterStage");
            modelIsReferenceComponent = CompileGetter<bool> (model, "IsReferenceComponent");
            modelSourcePartFlightIds = CompileGetter<object> (model, "SourcePartFlightIds");
            modelMass = CompileGetter<double> (model, "Mass");
            modelCenterOfMass = CompileGetter<Vector3d> (model, "CapturedCenterOfMass");
            modelReferenceRotation = CompileGetter<QuaternionD> (model, "CapturedReferenceRotation");

            CompileEvaluate (model, wrench, stateCtor);
            CompileDecomposeForce (analysis, forceMetrics);
            CompilePitchTrimSession (reentryAuthority, model, state, stateCtor);

            trimStep = CompileCall<bool> (trimSession, "Step");
            trimIsComplete = CompileGetter<bool> (trimSession, "IsComplete");
            trimEvaluationCount = CompileGetter<int> (trimSession, "EvaluationCount");
            trimResult = CompileGetter<object> (trimSession, "Result");
            trimResultSamples = CompileGetter<object> (trimResultType, "Samples");
            trimResultRoots = CompileGetter<object> (trimResultType, "Roots");
            trimResultSelectedRoot = CompileGetter<object> (trimResultType, "SelectedRoot");
            torqueSampleAngle = CompileGetter<double> (torqueSample, "Angle");
            torqueSampleTorque = CompileGetter<double> (torqueSample, "Torque");
            CompileTrimRoot (trimRootType, wrench, forceMetrics);

            createInertia = CompileConstructor<Func<double, double, double, double, double, double, object>> (
                inertia,
                new[] {
                    typeof(double), typeof(double), typeof(double),
                    typeof(double), typeof(double), typeof(double)
                });
            CompilePredictionParameters (predictionParameters, inertia);
            createPredictionSession = CompileConstructor<Func<object, object, object>> (
                predictionSession, new[] { model, predictionParameters });

            predictionStep = CompileCall<bool> (predictionSession, "Step");
            predictionIsComplete = CompileGetter<bool> (predictionSession, "IsComplete");
            predictionEvaluationCount = CompileGetter<int> (predictionSession, "EvaluationCount");
            predictionEquivalentCallCount = CompileGetter<int> (
                predictionSession, "EquivalentAerodynamicEndpointCallCount");
            predictionElapsedTime = CompileGetter<double> (predictionSession, "ElapsedTime");
            predictionCurrentAltitude = CompileGetter<double> (predictionSession, "CurrentAltitude");
            predictionSamples = CompileGetter<object> (predictionSession, "Samples");
            predictionResult = CompileGetter<object> (predictionSession, "Result");

            resultTermination = CompileGetter<int> (predictionResultType, "Termination");
            resultFinalState = CompileGetter<object> (predictionResultType, "FinalState");
            resultLatitude = CompileGetter<double> (predictionResultType, "LatitudeDegrees");
            resultLongitude = CompileGetter<double> (predictionResultType, "LongitudeDegrees");
            resultSurfaceDownrange = CompileGetter<double> (predictionResultType, "SurfaceDownrange");
            resultPeakDynamicPressure = CompileGetter<double> (predictionResultType, "PeakDynamicPressure");
            resultMaximumAngleOfAttack = CompileGetter<double> (predictionResultType, "MaximumAngleOfAttackDegrees");
            resultMaximumBodyRate = CompileGetter<double> (predictionResultType, "MaximumBodyRate");
            resultEvaluationCount = CompileGetter<int> (predictionResultType, "EvaluationCount");
            resultEquivalentCallCount = CompileGetter<int> (
                predictionResultType, "EquivalentAerodynamicEndpointCallCount");
            resultWallTime = CompileGetter<double> (predictionResultType, "WallTimeSeconds");
            resultSamples = CompileGetter<object> (predictionResultType, "Samples");

            sampleUniversalTime = CompileGetter<double> (predictionSample, "UniversalTime");
            sampleElapsedTime = CompileGetter<double> (predictionSample, "ElapsedTime");
            sampleAltitude = CompileGetter<double> (predictionSample, "Altitude");
            samplePosition = CompileGetter<Vector3d> (predictionSample, "BodyRelativePosition");
            sampleVelocity = CompileGetter<Vector3d> (predictionSample, "Velocity");
            sampleRotation = CompileGetter<QuaternionD> (predictionSample, "Rotation");
            sampleAngularVelocity = CompileGetter<Vector3d> (predictionSample, "AngularVelocity");
            sampleAirSpeed = CompileGetter<double> (predictionSample, "AirSpeed");
            sampleAngleOfAttack = CompileGetter<double> (predictionSample, "AngleOfAttackDegrees");
        }

        static void CompileEvaluate (Type modelType, Type wrenchType, ConstructorInfo stateCtor)
        {
            var model = Expression.Parameter (typeof(object), "model");
            var body = Expression.Parameter (typeof(CelestialBody), "body");
            var position = Expression.Parameter (typeof(Vector3d), "position");
            var velocity = Expression.Parameter (typeof(Vector3d), "velocity");
            var rotation = Expression.Parameter (typeof(QuaternionD), "rotation");
            var angularVelocity = Expression.Parameter (typeof(Vector3d), "angularVelocity");
            var ut = Expression.Parameter (typeof(double), "ut");
            var call = Expression.Call (
                Expression.Convert (model, modelType),
                GetInstanceMethod (modelType, "Evaluate"),
                Expression.New (stateCtor, body, position, velocity, rotation, angularVelocity, ut));
            var wrench = Expression.Variable (wrenchType, "wrench");
            var result = Expression.Block (
                new[] { wrench },
                Expression.Assign (wrench, call),
                Expression.New (
                    GetConstructor (typeof(Wrench), typeof(Vector3d), typeof(Vector3d)),
                    Expression.Property (wrench, "Force"),
                    Expression.Property (wrench, "Torque")));
            evaluate = Expression.Lambda<Func<object, CelestialBody, Vector3d, Vector3d, QuaternionD, Vector3d, double, Wrench>> (
                result, model, body, position, velocity, rotation, angularVelocity, ut).Compile ();
        }

        static void CompileDecomposeForce (Type analysisType, Type metricsType)
        {
            var force = Expression.Parameter (typeof(Vector3d), "force");
            var airRelativeVelocity = Expression.Parameter (typeof(Vector3d), "airRelativeVelocity");
            var call = Expression.Call (
                GetStaticMethod (analysisType, "DecomposeForce",
                    new[] { typeof(Vector3d), typeof(Vector3d) }),
                force, airRelativeVelocity);
            var metrics = Expression.Variable (metricsType, "metrics");
            var result = Expression.Block (
                new[] { metrics },
                Expression.Assign (metrics, call),
                Expression.New (
                    GetConstructor (typeof(ForceMetrics), typeof(double), typeof(double), typeof(double)),
                    Expression.Property (metrics, "Drag"),
                    Expression.Property (metrics, "Lift"),
                    Expression.Property (metrics, "LiftToDrag")));
            decomposeForce = Expression.Lambda<Func<Vector3d, Vector3d, ForceMetrics>> (
                result, force, airRelativeVelocity).Compile ();
        }

        static void CompilePitchTrimSession (Type authorityType, Type modelType, Type stateType, ConstructorInfo stateCtor)
        {
            var model = Expression.Parameter (typeof(object), "model");
            var body = Expression.Parameter (typeof(CelestialBody), "body");
            var position = Expression.Parameter (typeof(Vector3d), "position");
            var velocity = Expression.Parameter (typeof(Vector3d), "velocity");
            var rotation = Expression.Parameter (typeof(QuaternionD), "rotation");
            var angularVelocity = Expression.Parameter (typeof(Vector3d), "angularVelocity");
            var ut = Expression.Parameter (typeof(double), "ut");
            var call = Expression.Call (
                GetStaticMethod (authorityType, "CreateFlightPitchTrimSession",
                    new[] { modelType, stateType }),
                Expression.Convert (model, modelType),
                Expression.New (stateCtor, body, position, velocity, rotation, angularVelocity, ut));
            createPitchTrimSession = Expression.Lambda<Func<object, CelestialBody, Vector3d, Vector3d, QuaternionD, Vector3d, double, object>> (
                Expression.Convert (call, typeof(object)),
                model, body, position, velocity, rotation, angularVelocity, ut).Compile ();
        }

        static void CompileTrimRoot (Type rootType, Type wrenchType, Type metricsType)
        {
            var root = Expression.Parameter (typeof(object), "root");
            var typed = Expression.Convert (root, rootType);
            var wrench = Expression.Property (typed, "Wrench");
            var metrics = Expression.Property (typed, "Metrics");
            var result = Expression.New (
                GetConstructor (typeof(TrimRootData),
                    typeof(double), typeof(double), typeof(int), typeof(Wrench), typeof(ForceMetrics)),
                Expression.Property (typed, "Angle"),
                Expression.Property (typed, "Stiffness"),
                Expression.Convert (Expression.Property (typed, "Stability"), typeof(int)),
                Expression.New (
                    GetConstructor (typeof(Wrench), typeof(Vector3d), typeof(Vector3d)),
                    Expression.Property (wrench, "Force"),
                    Expression.Property (wrench, "Torque")),
                Expression.New (
                    GetConstructor (typeof(ForceMetrics), typeof(double), typeof(double), typeof(double)),
                    Expression.Property (metrics, "Drag"),
                    Expression.Property (metrics, "Lift"),
                    Expression.Property (metrics, "LiftToDrag")));
            trimRoot = Expression.Lambda<Func<object, TrimRootData>> (result, root).Compile ();
        }

        static void CompilePredictionParameters (Type parametersType, Type inertiaType)
        {
            var body = Expression.Parameter (typeof(CelestialBody), "body");
            var position = Expression.Parameter (typeof(Vector3d), "position");
            var velocity = Expression.Parameter (typeof(Vector3d), "velocity");
            var rotation = Expression.Parameter (typeof(QuaternionD), "rotation");
            var angularVelocity = Expression.Parameter (typeof(Vector3d), "angularVelocity");
            var ut = Expression.Parameter (typeof(double), "ut");
            var inertia = Expression.Parameter (typeof(object), "inertia");
            var stopAltitude = Expression.Parameter (typeof(double), "stopAltitude");
            var atmosphericTimeStep = Expression.Parameter (typeof(double), "atmosphericTimeStep");
            var vacuumTimeStep = Expression.Parameter (typeof(double), "vacuumTimeStep");
            var recordInterval = Expression.Parameter (typeof(double), "recordInterval");
            var maximumTime = Expression.Parameter (typeof(double), "maximumTime");
            var ctor = GetConstructor (parametersType,
                typeof(CelestialBody), typeof(Vector3d), typeof(Vector3d),
                typeof(QuaternionD), typeof(Vector3d), typeof(double), inertiaType,
                typeof(double), typeof(double), typeof(double), typeof(double), typeof(double));
            var call = Expression.New (ctor,
                body, position, velocity, rotation, angularVelocity, ut,
                Expression.Convert (inertia, inertiaType),
                stopAltitude, atmosphericTimeStep, vacuumTimeStep, recordInterval, maximumTime);
            createPredictionParameters = Expression.Lambda<Func<CelestialBody, Vector3d, Vector3d, QuaternionD, Vector3d, double, object, double, double, double, double, double, object>> (
                Expression.Convert (call, typeof(object)),
                body, position, velocity, rotation, angularVelocity, ut, inertia,
                stopAltitude, atmosphericTimeStep, vacuumTimeStep, recordInterval, maximumTime).Compile ();
        }

        static IList<double> FlattenSamples (IList samples, int start, int count)
        {
            if (start < 0 || count < 0 || start + count > samples.Count)
                throw new ArgumentOutOfRangeException (
                    nameof (start),
                    "The requested sample range [" + start + ", " + (start + count)
                    + ") is outside the recorded range [0, " + samples.Count + ").");
            var flattened = new List<double> (count * SampleStride);
            for (int i = start; i < start + count; ++i)
                AppendSample (flattened, samples [i]);
            return flattened;
        }

        static void AppendSample (IList<double> target, object sample)
        {
            target.Add (sampleUniversalTime (sample));
            target.Add (sampleElapsedTime (sample));
            target.Add (sampleAltitude (sample));
            var position = samplePosition (sample);
            target.Add (position.x);
            target.Add (position.y);
            target.Add (position.z);
            var velocity = sampleVelocity (sample);
            target.Add (velocity.x);
            target.Add (velocity.y);
            target.Add (velocity.z);
            var rotation = sampleRotation (sample);
            target.Add (rotation.x);
            target.Add (rotation.y);
            target.Add (rotation.z);
            target.Add (rotation.w);
            var angularVelocity = sampleAngularVelocity (sample);
            target.Add (angularVelocity.x);
            target.Add (angularVelocity.y);
            target.Add (angularVelocity.z);
            target.Add (sampleAirSpeed (sample));
            target.Add (sampleAngleOfAttack (sample));
        }

        static Type GetApiType (Assembly assembly, string name)
        {
            var type = assembly.GetType ("KSPAeroSim." + name);
            if (type == null)
                throw new MissingMemberException ("type KSPAeroSim." + name + " not found");
            return type;
        }

        static PropertyInfo GetStaticProperty (Type type, string name)
        {
            var property = type.GetProperty (name, BindingFlags.Public | BindingFlags.Static);
            if (property == null)
                throw new MissingMemberException (type.Name + "." + name + " not found");
            return property;
        }

        static MethodInfo GetInstanceMethod (Type type, string name)
        {
            var method = type.GetMethod (name, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                throw new MissingMemberException (type.Name + "." + name + " not found");
            return method;
        }

        static MethodInfo GetStaticMethod (Type type, string name, Type[] parameterTypes)
        {
            var method = type.GetMethod (
                name, BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null);
            if (method == null)
                throw new MissingMemberException (type.Name + "." + name + " not found");
            return method;
        }

        static ConstructorInfo GetConstructor (Type type, params Type[] parameterTypes)
        {
            var ctor = type.GetConstructor (parameterTypes);
            if (ctor == null)
                throw new MissingMemberException (type.Name + " constructor not found");
            return ctor;
        }

        static void CheckEnum (Type type, params string[] names)
        {
            for (int i = 0; i < names.Length; ++i) {
                if (Enum.GetName (type, i) != names [i])
                    throw new MissingMemberException (
                        type.Name + " value " + i + " is not " + names [i]);
            }
        }

        static Expression Coerce (Expression expression, Type target)
        {
            return expression.Type == target ? expression : Expression.Convert (expression, target);
        }

        static Func<object, T> CompileGetter<T> (Type type, string name)
        {
            var obj = Expression.Parameter (typeof(object), "obj");
            var property = type.GetProperty (name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new MissingMemberException (type.Name + "." + name + " not found");
            var value = Expression.Property (Coerce (obj, type), property);
            return Expression.Lambda<Func<object, T>> (Coerce (value, typeof(T)), obj).Compile ();
        }

        static Func<T> CompileStaticGetter<T> (Type type, string name)
        {
            var value = Expression.Property (null, GetStaticProperty (type, name));
            return Expression.Lambda<Func<T>> (Coerce (value, typeof(T))).Compile ();
        }

        static Func<object, T> CompileCall<T> (Type type, string name)
        {
            var obj = Expression.Parameter (typeof(object), "obj");
            var value = Expression.Call (Coerce (obj, type), GetInstanceMethod (type, name));
            return Expression.Lambda<Func<object, T>> (Coerce (value, typeof(T)), obj).Compile ();
        }

        static TDelegate CompileStaticCall<TDelegate> (Type type, string name, Type[] parameterTypes)
        {
            var method = GetStaticMethod (type, name, parameterTypes);
            var invoke = typeof(TDelegate).GetMethod ("Invoke");
            var parameters = invoke.GetParameters ()
                .Select (parameter => Expression.Parameter (parameter.ParameterType)).ToArray ();
            var arguments = new Expression[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; ++i)
                arguments [i] = Coerce (parameters [i], parameterTypes [i]);
            return Expression.Lambda<TDelegate> (
                Coerce (Expression.Call (method, arguments), invoke.ReturnType),
                parameters).Compile ();
        }

        static TDelegate CompileConstructor<TDelegate> (Type type, Type[] parameterTypes)
        {
            var ctor = GetConstructor (type, parameterTypes);
            var invoke = typeof(TDelegate).GetMethod ("Invoke");
            var parameters = invoke.GetParameters ()
                .Select (parameter => Expression.Parameter (parameter.ParameterType)).ToArray ();
            var arguments = new Expression[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; ++i)
                arguments [i] = Coerce (parameters [i], parameterTypes [i]);
            return Expression.Lambda<TDelegate> (
                Coerce (Expression.New (ctor, arguments), invoke.ReturnType),
                parameters).Compile ();
        }

        static void Error (string message)
        {
            KRPC.Utils.Logger.WriteLine ("Load API: " + message, KRPC.Utils.Logger.Severity.Error);
            Compatibility.SpawnPopupDialog (
                new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), "krpc-api-loader",
                "kRPC API Loader", message, "OK", true, HighLogic.UISkin);
        }
    }
}
