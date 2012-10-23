using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Preserves metadata tokens
	/// </summary>
	sealed class PreserveTokensMetaData : MetaData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		/// <param name="options">Options</param>
		public PreserveTokensMetaData(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetaDataOptions options)
			: base(module, constants, methodBodies, netResources, options) {
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeRef tr) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeDef td) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(FieldDef fd) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(MethodDef md) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(ParamDef pd) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(MemberRef mr) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(StandAloneSig sas) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(EventDef ed) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(PropertyDef pd) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeSpec ts) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		public override uint GetRid(MethodSpec ms) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override IEnumerable<TypeDef> GetAllTypeDefs() {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override void AllocateTypeDefRids() {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override void AllocateMemberDefRids() {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override uint AddTypeRef(TypeRef tr) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override uint AddTypeSpec(TypeSpec ts) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override uint AddMemberRef(MemberRef mr) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override uint AddStandAloneSig(StandAloneSig sas) {
			throw new NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override uint AddMethodSpec(MethodSpec ms) {
			throw new NotImplementedException();	//TODO:
		}
	}
}
