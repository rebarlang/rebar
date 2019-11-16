using System;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class BuildTupleNode : DfirNode
    {
        public BuildTupleNode(Diagram parentDiagram, int inputCount)
            : base(parentDiagram)
        {
            CreateTerminal(Direction.Output, PFTypes.Void, "out");
            for (int i = 0; i < inputCount; ++i)
            {
                CreateTerminal(Direction.Input, PFTypes.Void, $"in_{i}");
            }
        }

        private BuildTupleNode(Node parentNode, BuildTupleNode copyFrom, NodeCopyInfo copyInfo)
            : base(parentNode, copyFrom, copyInfo)
        {
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitBuildTupleNode(this);
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new BuildTupleNode(newParentNode, this, copyInfo);
        }
    }
}
