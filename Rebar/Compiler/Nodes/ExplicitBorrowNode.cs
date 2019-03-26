using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// Represents a node that will output references in a common lifetime from its inputs. The <see cref="AlwaysBeginLifetime"/>
    /// and <see cref="AlwaysCreateReference"/> properties govern the content and lifetime of the output references.
    /// </summary>
    internal class ExplicitBorrowNode : DfirNode
    {
        public ExplicitBorrowNode(Node parentNode, BorrowMode borrowMode, int inputs, bool alwaysBeginLifetime, bool alwaysCreateReference)
            : base(parentNode)
        {
            BorrowMode = borrowMode;
            NIType inputType = PFTypes.Void, outputType;
            switch (borrowMode)
            {
                case BorrowMode.Mutable:
                    outputType = PFTypes.Void.CreateMutableReference();
                    break;
                default:
                    outputType = PFTypes.Void.CreateImmutableReference();
                    break;
            }
            for (int i = 0; i < inputs; ++i)
            {
                CreateTerminal(Direction.Input, inputType, $"in_{i}");
            }
            for (int i = 0; i < inputs; ++i)
            {
                CreateTerminal(Direction.Output, outputType, $"out_{i}");
            }
        }

        private ExplicitBorrowNode(Node parentNode, ExplicitBorrowNode copyFrom, NodeCopyInfo copyInfo)
            : base(parentNode, copyFrom, copyInfo)
        {
            BorrowMode = copyFrom.BorrowMode;
            AlwaysBeginLifetime = copyFrom.AlwaysBeginLifetime;
            AlwaysCreateReference = copyFrom.AlwaysCreateReference;
        }

        /// <summary>
        /// The <see cref="BorrowMode"/> with which to create the output references.
        /// </summary>
        public BorrowMode BorrowMode { get; }

        /// <summary>
        /// Whether to begin a new lifetime unconditionally for the outputs.
        /// If true, or if false and the inputs are not already in a common bounded lifetime: 
        ///     the outputs will be in a new common diagram-bounded lifetime that interrupts each of the inputs.
        /// Otherwise:
        ///     the outputs will be in the common lifetime of the inputs.
        /// </summary>
        public bool AlwaysBeginLifetime { get; }

        /// <summary>
        /// Whether to create references to the inputs unconditionally.
        /// If true, each output will be a reference to its input.
        /// If false, each output will be the same reference as its input if its input is a reference, and otherwise a reference to its input.
        /// </summary>
        public bool AlwaysCreateReference { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExplicitBorrowNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitExplicitBorrowNode(this);
        }
    }
}
