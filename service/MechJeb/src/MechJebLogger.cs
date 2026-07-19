using System;

using KRPC.Utils;

namespace KRPC.MechJeb {
	internal static class MechJebLogger {
		internal static void Debug(string message) {
			Write(message, Logger.Severity.Debug);
		}

		internal static void Info(string message) {
			Write(message, Logger.Severity.Info);
		}

		internal static void Warning(string message) {
			Write(message, Logger.Severity.Warning);
		}

		internal static void Warning(string message, Exception ex) {
			Write(message, Logger.Severity.Warning, ex);
		}

		internal static void Error(string message) {
			Write(message, Logger.Severity.Error);
		}

		internal static void Error(string message, Exception ex) {
			Write(message, Logger.Severity.Error, ex);
		}

		static void Write(string message, Logger.Severity severity, Exception ex = null) {
			var detail = ex == null ? string.Empty : Environment.NewLine + ex;
			Logger.WriteLine("MechJeb - " + message + detail, severity);
		}
	}
}
