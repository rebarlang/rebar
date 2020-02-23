using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;
using Rebar.RebarTarget.LLVM;

namespace Rebar.RebarTarget
{
    internal class FunctionCompileSignature : CompileSignature
    {
        public FunctionCompileSignature(
            ExtendedQualifiedName functionName,
            IEnumerable<CompileSignatureParameter> compileSignatureParameters,
            bool isYielding,
            bool mayPanic)
            : base(functionName: functionName,
                parameters: compileSignatureParameters,
                declaringType: NIType.Unset,
                reentrancy: Reentrancy.None,
                isYielding: isYielding,
                isFunctional: true,
                threadAffinity: ThreadAffinity.Standard,
                shouldAlwaysInline: false,
                mayWantToInline: true,
                priority: ExecutionPriority.Normal,
                callingConvention: System.Runtime.InteropServices.CallingConvention.StdCall)
        {
            Version = FunctionBuiltPackage.CurrentVersion;
            MayPanic = mayPanic;
        }

        public FunctionCompileSignature(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Version = (Version)info.GetValue(nameof(Version), typeof(Version));
            MayPanic = info.GetBoolean(nameof(MayPanic));
        }

        public Version Version { get; }

        public bool MayPanic { get; }
    }
}
