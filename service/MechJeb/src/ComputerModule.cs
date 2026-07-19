using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb {
	/// <summary>
	/// Provides access to MechJeb's <c>Module</c> functionality.
	/// </summary>
	public abstract class Module {
		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal abstract void InitInstance(object instance);
	}

	/// <summary>
	/// Provides access to MechJeb's <c>ComputerModule</c> functionality.
	/// </summary>
	public abstract class ComputerModule : Module {
		internal const string MechJebType = "MuMech.ComputerModule";

		// Methods needed for correct functionalify
		private static MethodInfo onFixedUpdate;

		// Fields and methods
		private static PropertyInfo enabled;
		private static FieldInfo usersField;

		// Instance objects
		/// <summary>
		/// Gets the value of <c>instance</c> in MechJeb.
		/// </summary>
		protected internal object instance;

		private object users;

		internal static void InitType(Type type) {
			onFixedUpdate = type.GetCheckedMethod("OnFixedUpdate");

			enabled = type.GetCheckedProperty("Enabled");
			usersField = type.GetCheckedField("Users");
		}

		/// <summary>
		/// Initializes this wrapper from the reflected MechJeb instance.
		/// </summary>
		protected internal override void InitInstance(object instance) {
			this.instance = instance;

			this.users = usersField.GetInstanceValue(instance);
		}

		/// <summary>
		/// Gets or sets the value of <c>Enabled</c> in MechJeb.
		/// </summary>
		public virtual bool Enabled {
			get => (bool)enabled.GetValue(this.instance, null);
			set {
				if(value)
					UserPool.usersAdd.Invoke(this.users, new object[] { this });
				else
					UserPool.usersRemove.Invoke(this.users, new object[] { this });
			}
		}

		internal void OnFixedUpdate() {
			onFixedUpdate.Invoke(this.instance, null);
		}

		internal static bool IsEnabled(object moduleInstance) {
			return moduleInstance != null && (bool)enabled.GetValue(moduleInstance, null);
		}

		internal static void SetEnabled(object moduleInstance, object caller, bool value) {
			if(moduleInstance == null)
				return;
			object usersObj = usersField.GetValue(moduleInstance);
			if(value)
				UserPool.usersAdd.Invoke(usersObj, new object[] { caller });
			else
				UserPool.usersRemove.Invoke(usersObj, new object[] { caller });
		}

		private static class UserPool {
			internal const string MechJebType = "MuMech.UserPool";

			internal static MethodInfo usersAdd;
			internal static MethodInfo usersRemove;

			internal static void InitType(Type type) {
				usersAdd = type.GetCheckedMethod("Add");
				usersRemove = type.GetCheckedMethod("Remove");
			}
		}
	}

	/// <summary>
	/// Provides access to MechJeb's <c>KRPCComputerModule</c> functionality.
	/// </summary>
	public abstract class KRPCComputerModule : ComputerModule {
		/// <summary>
		/// Gets or sets whether this MechJeb computer module is enabled.
		/// </summary>
		[KRPCProperty]
		public override bool Enabled {
			get => base.Enabled;
			set => base.Enabled = value;
		}
	}

	/// <summary>
	/// Provides access to MechJeb's <c>AutopilotModule</c> functionality.
	/// </summary>
	public abstract class AutopilotModule : KRPCComputerModule {
		internal new const string MechJebType = "MuMech.AutopilotModule";

		// Fields and methods
		internal static PropertyInfo status;

		/// <summary>
		/// The current status reported by this MechJeb module.
		/// </summary>
		[KRPCProperty]
		public string Status => (string)status.GetValue(this.instance, null);

		internal static new void InitType(Type type) {
			status = type.GetCheckedProperty("Status");
		}
	}

	/// <summary>
	/// Provides access to MechJeb's <c>DisplayModule</c> functionality.
	/// </summary>
	public abstract class DisplayModule : ComputerModule { }
}
