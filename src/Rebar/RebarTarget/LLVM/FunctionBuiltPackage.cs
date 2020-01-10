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
        private static readonly Version CurrentVersion = new Version(0, 1, 1, 0);
        private static readonly Version CommonModuleDependenciesVersion = new Version(0, 1, 1, 0);

        public FunctionBuiltPackage(
            SpecAndQName identity,
            QualifiedName targetIdentity,
            SpecAndQName[] dependencyIdentities,
            Module module,
            string[] commonModuleDependencies)
        {
            RuntimeEntityIdentity = identity;
            TargetIdentity = targetIdentity;
            DependencyIdentities = dependencyIdentities;
            Module = module;
            CommonModuleDependencies = commonModuleDependencies;
        }

        protected FunctionBuiltPackage(SerializationInfo info, StreamingContext context)
        {
            Version version = new Version(0, 1, 0, 0);
            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == nameof(Version))
                {
                    version = (Version)entry.Value;
                    break;
                }
            }

            RuntimeEntityIdentity = (SpecAndQName)info.GetValue(nameof(RuntimeEntityIdentity), typeof(SpecAndQName));
            TargetIdentity = (QualifiedName)info.GetValue(nameof(TargetIdentity), typeof(QualifiedName));
            DependencyIdentities = (SpecAndQName[])info.GetValue(nameof(DependencyIdentities), typeof(SpecAndQName[]));
            Token = (BuiltPackageToken)info.GetValue(nameof(Token), typeof(BuiltPackageToken));
            byte[] moduleBytes = (byte[])info.GetValue(nameof(Module), typeof(byte[]));
            Module = moduleBytes.DeserializeModuleAsBitcode();

            if (version >= CommonModuleDependenciesVersion)
            {
                CommonModuleDependencies = (string[])info.GetValue(nameof(CommonModuleDependencies), typeof(string[]));
            }
            else
            {
                // Assume all common modules that existed before CommonModuleDependenciesVersion are needed
                CommonModuleDependencies = new string[] { "fakedrop", "scheduler", "string", "range", "file" };
            }
        }

        public Module Module { get; }

        public bool IsPackageValid => true;

        public IRuntimeEntityIdentity RuntimeEntityIdentity { get; }

        public QualifiedName TargetIdentity { get; }

        public BuiltPackageToken Token { get; set; }

        public string[] CommonModuleDependencies { get; }

        private SpecAndQName[] DependencyIdentities { get; }

        public byte[] GetBinary()
        {
            // I can't tell that this will ever actually be used. It's definitely not used for serializing the BuiltPackage.
            return null;
        }

        public IEnumerable<IRuntimeEntityIdentity> GetDependencies() => DependencyIdentities ?? Enumerable.Empty<IRuntimeEntityIdentity>();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(RuntimeEntityIdentity), RuntimeEntityIdentity);
            info.AddValue(nameof(TargetIdentity), TargetIdentity);
            info.AddValue(nameof(DependencyIdentities), DependencyIdentities);
            info.AddValue(nameof(Token), Token);
            info.AddValue(nameof(Module), Module.SerializeModuleAsBitcode());
        }
    }
}