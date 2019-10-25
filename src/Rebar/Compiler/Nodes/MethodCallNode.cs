using System.Collections.Generic;
using NationalInstruments.Dfir;
using NationalInstruments.DataTypes;
using NationalInstruments.Linking;
using NationalInstruments.Compiler;

namespace Rebar.Compiler.Nodes
{
    internal class MethodCallNode : DfirNode
    {
        private readonly List<PassthroughTerminalPair> _passthroughTerminalPairs;
        private readonly Dictionary<Terminal, NIType> _terminalParameters = new Dictionary<Terminal, NIType>();

        public MethodCallNode(Node parentNode, ExtendedQualifiedName targetName, NIType signature)
            : base(parentNode, EnumerateRequiredDependencies(new SpecAndQName(parentNode.DfirRoot.BuildSpec, targetName), signature))
        {
            TargetName = targetName;
            Signature = signature;
            foreach (var parameter in signature.GetParameters())
            {
                NIType dataType = parameter.GetDataType();
                string name = parameter.GetUserVisibleParameterName();
                Terminal inputTerminal, outputTerminal;
                if (parameter.IsInputOnlyParameter())
                {
                    inputTerminal = CreateTerminal(Direction.Input, dataType, name);
                    _terminalParameters[inputTerminal] = parameter;
                }
                else if (parameter.IsOutputOnlyParameter())
                {
                    outputTerminal = CreateTerminal(Direction.Output, dataType, name);
                    _terminalParameters[outputTerminal] = parameter;
                }
                else
                {
                    inputTerminal = CreateTerminal(Direction.Input, dataType, name);
                    outputTerminal = CreateTerminal(Direction.Output, dataType, name);
                    _terminalParameters[inputTerminal] = parameter;
                    _terminalParameters[outputTerminal] = parameter;
                    _passthroughTerminalPairs = _passthroughTerminalPairs ?? new List<PassthroughTerminalPair>();
                    _passthroughTerminalPairs.Add(new PassthroughTerminalPair(inputTerminal, outputTerminal));
                }
            }
        }

        private MethodCallNode(Node parentNode, MethodCallNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
            TargetName = nodeToCopy.TargetName;
            Signature = nodeToCopy.Signature;
        }

        private static IEnumerable<DfirDependency> EnumerateRequiredDependencies(SpecAndQName targetSpecAndQName, NIType targetSignature)
        {
            yield return new SignatureDependency(targetSpecAndQName, targetSignature);
            yield return new RunnableExistenceDependency(targetSpecAndQName);
        }

        public ExtendedQualifiedName TargetName { get; }

        public NIType Signature { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MethodCallNode(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitMethodCallNode(this);
        }
    }
}
