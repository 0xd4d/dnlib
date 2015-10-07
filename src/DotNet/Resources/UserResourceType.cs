// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// User resource type
	/// </summary>
	public sealed class UserResourceType {
		readonly string name;
		readonly ResourceTypeCode code;

		/// <summary>
		/// Full name including assembly of type
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// User type code
		/// </summary>
		public ResourceTypeCode Code {
			get { return code; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Full name including assembly of type</param>
		/// <param name="code">User type code</param>
		public UserResourceType(string name, ResourceTypeCode code) {
			this.name = name;
			this.code = code;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0:X2} {1}", (int)code, name);
		}
	}
}
