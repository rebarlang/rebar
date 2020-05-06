using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using NationalInstruments.Linking;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Rebar's implementation of compile target class. Target Compilers are responsible for turning DFIR into compiled code.
    /// </summary>
    public class TargetCompiler : DelegatingTargetCompiler
    {
        /// <summary>
        /// Target kind identifier for the Rebar target.
        /// </summary>
        public const string Kind = "NationalInstruments.Target.RebarTarget";

        /// <summary>
        /// Construct a new Rebar target compiler
        /// </summary>
        /// <param name="project">project</param>
        /// <param name="host">The composition host</param>
        /// <param name="targetIdentity">The qualified name of the target</param>
        /// <param name="compileCache">The compile cache</param>
        /// <param name="factories">Factories for compilers for different file types</param>
        public TargetCompiler(
            Project project,
            ICompositionHost host,
            TargetCompilerIdentity targetIdentity,
            IPersistentCache compileCache,
            IEnumerable<ITargetCompileHandlerFactory> factories,
            ITargetCompilerLookup targetCompilerLookup)
            : base(
                  host.GetSharedExportedValue<AnyMocCompiler>(),
                  host.GetSharedExportedValue<ScheduledActivityManager>(),
                  compileCache,
                  targetIdentity,
                  factories,
                  targetCompilerLookup)
        {
            Project = project;
        }

        /// <summary>
        /// the project which owns this compiler
        /// </summary>
        public Project Project { get; }

        /// <inheritdoc/>
        public override CodeId TargetCodeId => Environment.Is64BitProcess ? CodeId.kx86Win64CodeID : CodeId.kx86WinCodeID;

        /// <inheritdoc/>
        public override BuildSpec CreateDefaultBuildSpec(CompilableDefinitionName topLevelDefinitionName, IReadOnlySymbolTable symbolTable)
        {
            Log.Assert(0xC3B662C7U, topLevelDefinitionName.OwningComponentRuntimeName != null, "Component name must be set in ExtendedQualifiedNames passed to compiler");
            return new BuildSpec(TargetCompilerIdentity, symbolTable);
        }

        /// <inheritdoc/>
        public override string TargetKind => Kind;

        /// <inheritdoc/>
        public override ITargetTypeSerializer TargetTypeSerializer
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public override IEnumerable<string> GetReservedPropertyNames()
        {
            return Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public override Task TargetCheckNodeAsync(Node targetDfirNode)
        {
            return AsyncHelpers.CompletedTask;
        }

        /// <inheritdoc/>
        public override bool IsRecursionSupported()
        {
            return false;
        }

        /// <inheritdoc/>
        public override bool SupportsErrorTerminalsOnPropertyNode => true;

        /// <inheritdoc/>
        public override bool SupportsDebugging => false;

        /// <inheritdoc/>
        public override int MaximumInferredFixedSizeArraySize => -1;

        /// <inheritdoc/>
        public override string ConvertToValidCompiledName(string name)
        {
            return "dataItem_" + base.ConvertToValidCompiledName(name);
        }
    }
}
