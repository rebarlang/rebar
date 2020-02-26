using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using LLVMSharp;
using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.ExecutionFramework;

namespace Rebar.RebarTarget.LLVM
{
    [Serializable]
    public class FunctionBuiltPackage : IBuiltPackage, ISerializable
    {
        /// <remarks>
        /// This value should never change; it represents the version that built packages that did not serialize a version
        /// are considered to have.
        /// 
        /// In the _untracked version, the properties RuntimeEntityIdentity, TargetIdentity, DependencyIdentities, Token,
        /// and Module are assumed to exist.
        /// </remarks>
        private static readonly Version _untrackedVersion = new Version(0, 1, 0, 0);

        /// <summary>
        /// Minimum version that is considered loadable and/or valid.
        /// </summary>
        internal static Version MinimumLoadableVersion => AddIsYieldingVersion;

        /// <summary>
        /// Version at which FunctionCompileSignature replaced CompileSignature for Functions and when
        /// the MayPanic flag was added to FunctionCompileSignature.
        /// </summary>
        internal static readonly Version FunctionMayPanicVersion = new Version(0, 1, 1, 0);

        /// <summary>
        /// Version at which the value of IsYielding is stored in the FunctionBuiltPackage; compiled Functions no longer
        /// have an outer function, and the ::Poll function for async Functions takes the caller waker function and state
        /// pointers.
        /// </summary>
        internal static readonly Version AddIsYieldingVersion = new Version(0, 1, 2, 0);

        /// <summary>
        /// The current/latest version of the built package, given to all newly created built packages.
        /// </summary>
        /// <remarks>This should change whenever the built package format changes; it should always be the value
        /// of another readonly Version property that describes what changed at that version.</remarks>
        internal static Version CurrentVersion => AddIsYieldingVersion;

        public FunctionBuiltPackage(
            SpecAndQName identity,
            QualifiedName targetIdentity,
            SpecAndQName[] dependencyIdentities,
            Module module,
            bool isYielding)
        {
            Version = CurrentVersion;
            RuntimeEntityIdentity = identity;
            TargetIdentity = targetIdentity;
            DependencyIdentities = dependencyIdentities;
            Module = module;
            IsYielding = isYielding;
        }

        protected FunctionBuiltPackage(SerializationInfo info, StreamingContext context)
        {
            Version = DeserializeVersion(info);

            // if (Version >= _untrackedVersion) // always true
            RuntimeEntityIdentity = (SpecAndQName)info.GetValue(nameof(RuntimeEntityIdentity), typeof(SpecAndQName));
            TargetIdentity = (QualifiedName)info.GetValue(nameof(TargetIdentity), typeof(QualifiedName));
            DependencyIdentities = (SpecAndQName[])info.GetValue(nameof(DependencyIdentities), typeof(SpecAndQName[]));
            Token = (BuiltPackageToken)info.GetValue(nameof(Token), typeof(BuiltPackageToken));
            byte[] moduleBytes = (byte[])info.GetValue(nameof(Module), typeof(byte[]));
            Module = moduleBytes.DeserializeModuleAsBitcode();

            if (Version >= AddIsYieldingVersion)
            {
                IsYielding = info.GetBoolean(nameof(IsYielding));
            }
        }

        private static Version DeserializeVersion(SerializationInfo info)
        {
            Version version = _untrackedVersion;
            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == nameof(Version))
                {
                    version = (Version)entry.Value;
                    break;
                }
            }
            return version;
        }

        public Version Version { get; }

        public Module Module { get; }

        public bool IsYielding { get; }

        public bool IsPackageValid => Version >= MinimumLoadableVersion;

        public IRuntimeEntityIdentity RuntimeEntityIdentity { get; }

        public QualifiedName TargetIdentity { get; }

        public BuiltPackageToken Token { get; set; }

        private SpecAndQName[] DependencyIdentities { get; }

        public byte[] GetBinary()
        {
            // I can't tell that this will ever actually be used. It's definitely not used for serializing the BuiltPackage.
            return null;
        }

        public IEnumerable<IRuntimeEntityIdentity> GetDependencies() => DependencyIdentities ?? Enumerable.Empty<IRuntimeEntityIdentity>();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Version), Version);
            info.AddValue(nameof(RuntimeEntityIdentity), RuntimeEntityIdentity);
            info.AddValue(nameof(TargetIdentity), TargetIdentity);
            info.AddValue(nameof(DependencyIdentities), DependencyIdentities);
            info.AddValue(nameof(Token), Token);
            info.AddValue(nameof(Module), Module.SerializeModuleAsBitcode());
            info.AddValue(nameof(IsYielding), IsYielding);
        }
    }
}