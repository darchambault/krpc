using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KRPC
{
    /// <summary>
    /// Warns when KSP loads the legacy standalone KRPC.MechJeb add-on, or when
    /// multiple loadable copies of the add-on are present in GameData.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.MainMenu, true)]
    internal sealed class MechJebInstallationChecker : MonoBehaviour
    {
        const string AssemblyName = "KRPC.MechJeb";
        const string AssemblyFileName = AssemblyName + ".dll";
        const string IntegrationMarkerType = "KRPC.MechJeb.IntegratedServiceMarker";

        /// <summary>
        /// Checks the loaded assembly and displays an actionable warning if necessary.
        /// </summary>
        public void Start ()
        {
            Addon.InitLogger ();

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies ()
                .Where (assembly => assembly.GetName ().Name == AssemblyName)
                .ToList ();
            var gameDataPath = Path.GetFullPath (Path.Combine (KSPUtil.ApplicationRootPath, "GameData"));
            var installedFiles = FindInstalledFiles (gameDataPath);

            var legacyLoaded = loadedAssemblies.Any (
                assembly => assembly.GetType (IntegrationMarkerType, false) == null);
            var multipleAssembliesLoaded = loadedAssemblies.Count > 1;
            var multipleFilesInstalled = installedFiles.Count > 1;
            if (!legacyLoaded && !multipleAssembliesLoaded && !multipleFilesInstalled)
                return;

            var message = BuildMessage (
                loadedAssemblies, installedFiles, gameDataPath,
                legacyLoaded, multipleAssembliesLoaded, multipleFilesInstalled);
            Utils.Logger.WriteLine (message, Utils.Logger.Severity.Warning);
            Utils.Compatibility.SpawnPopupDialog (
                new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f),
                "krpc-mechjeb-installation-warning", "kRPC MechJeb Installation Warning",
                message, "OK", false, HighLogic.UISkin);
        }

        static List<string> FindInstalledFiles (string gameDataPath)
        {
            try {
                return Directory.GetFiles (gameDataPath, "*.dll", SearchOption.AllDirectories)
                    .Where (path => string.Equals (
                        Path.GetFileName (path), AssemblyFileName, StringComparison.OrdinalIgnoreCase))
                    .Where (path => !IsInPluginData (path))
                    .Select (Path.GetFullPath)
                    .OrderBy (path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList ();
            } catch (Exception exn) {
                Utils.Logger.WriteLine (
                    "Failed to search GameData for " + AssemblyFileName + ": " + exn,
                    Utils.Logger.Severity.Warning);
                return new List<string> ();
            }
        }

        static bool IsInPluginData (string path)
        {
            var directory = Path.GetDirectoryName (path);
            while (!string.IsNullOrEmpty (directory)) {
                if (string.Equals (
                    Path.GetFileName (directory), "PluginData", StringComparison.OrdinalIgnoreCase))
                    return true;
                directory = Path.GetDirectoryName (directory);
            }
            return false;
        }

        static string BuildMessage (
            IList<Assembly> loadedAssemblies, IList<string> installedFiles, string gameDataPath,
            bool legacyLoaded, bool multipleAssembliesLoaded, bool multipleFilesInstalled)
        {
            var message = new StringBuilder ();
            if (legacyLoaded)
                message.AppendLine (
                    "KSP loaded a legacy standalone KRPC.MechJeb add-on. It may take precedence over the version included with kRPC.");
            if (multipleAssembliesLoaded)
                message.AppendLine ("KSP loaded more than one KRPC.MechJeb assembly.");
            if (multipleFilesInstalled)
                message.AppendLine ("Multiple loadable copies of KRPC.MechJeb.dll were found in GameData.");

            if (loadedAssemblies.Count > 0) {
                message.AppendLine ();
                message.AppendLine (loadedAssemblies.Count == 1 ? "Loaded assembly:" : "Loaded assemblies:");
                foreach (var assembly in loadedAssemblies) {
                    var version = assembly.GetName ().Version;
                    message.Append ("  ")
                        .Append (DisplayPath (assembly.Location, gameDataPath))
                        .Append (" (version ")
                        .Append (version)
                        .AppendLine (")");
                }
            }

            if (installedFiles.Count > 0) {
                message.AppendLine ();
                message.AppendLine ("Installed copies:");
                foreach (var path in installedFiles)
                    message.Append ("  ").AppendLine (DisplayPath (path, gameDataPath));
            }

            message.AppendLine ();
            message.AppendLine (
                "Exit KSP, remove standalone copies of KRPC.MechJeb.dll, then reinstall the current kRPC release so that only GameData/kRPC/KRPC.MechJeb.dll remains. Restart KSP afterwards.");
            message.Append (
                "kRPC will remain enabled, but the MechJeb service may be outdated or unreliable in this session.");
            return message.ToString ();
        }

        static string DisplayPath (string path, string gameDataPath)
        {
            if (string.IsNullOrEmpty (path))
                return "unknown path";

            var fullPath = Path.GetFullPath (path);
            var gameDataPrefix = gameDataPath.TrimEnd (
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith (gameDataPrefix, StringComparison.OrdinalIgnoreCase))
                return "GameData/" + fullPath.Substring (gameDataPrefix.Length).Replace ('\\', '/');
            return fullPath;
        }
    }
}
