using System;
using System.Runtime.Serialization;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    /// <summary>
    /// Wrapper around a serialized/context-free LLVM module.
    /// </summary>
    /// <remarks>
    /// <see cref="Module"/>s are normally bound to a <see cref="LLVMContextRef"/>. In order to compile functions and other
    /// entities in a form that can be reused across contexts, it is useful to hold onto them in serialized form, which can
    /// then be deserialized into whatever context necessary.
    /// </remarks>
    [Serializable]
    public class ContextFreeModule : ISerializable
    {
        /// <summary>
        /// Construct a <see cref="ContextFreeModule"/> from a <see cref="Module"/>.
        /// </summary>
        /// <param name="module">The <see cref="Module"/> to create a context-free version of.</param>
        public ContextFreeModule(Module module)
        {
            SerializedModule = module.SerializeModuleAsBitcode();
        }

        /// <summary>
        /// Construct a <see cref="ContextFreeModule"/> from the serialized bytes of a module.
        /// </summary>
        /// <param name="serializedModule">The serialized bytes of a module.</param>
        public ContextFreeModule(byte[] serializedModule)
        {
            SerializedModule = serializedModule;
        }

        private byte[] SerializedModule { get; }

        /// <summary>
        /// Serialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ContextFreeModule(SerializationInfo info, StreamingContext context)
        {
            SerializedModule = (byte[])info.GetValue(nameof(SerializedModule), typeof(byte[]));
        }

        /// <inheritdoc />
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(SerializedModule), SerializedModule);
        }

        /// <summary>
        /// Loads this module as a <see cref="Module"/> in the given <see cref="LLVMContextRef"/>.
        /// </summary>
        /// <param name="context">The <see cref="LLVMContextRef"/> to load the module into.</param>
        /// <returns>A <see cref="Module"/> bound to the given <see cref="LLVMContextRef"/>.</returns>
        internal Module LoadModuleInContext(LLVMContextRef context)
        {
            return SerializedModule.DeserializeModuleAsBitcode(context);
        }
    }
}
