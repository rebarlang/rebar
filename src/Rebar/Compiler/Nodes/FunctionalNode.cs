using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation of a <see cref="SourceModel.FunctionalNode"/>.
    /// </summary>
    internal class FunctionalNode : DfirNode
    {
        public FunctionalNode(Node parent, NIType signature, IEnumerable<string> requiredFeatureToggles = null)
            : base(parent)
        {
            Signature = signature;
            CreateTerminalsFromSignature(Signature);
            RequiredFeatureToggles = requiredFeatureToggles ?? Enumerable.Empty<string>();
        }

        private FunctionalNode(Node parentNode, FunctionalNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
            Signature = nodeToCopy.Signature;
            FunctionType = nodeToCopy.FunctionType;
            RequiredFeatureToggles = nodeToCopy.RequiredFeatureToggles;
        }

        public NIType Signature { get; }

        internal FunctionType FunctionType { get; set; }

        public IEnumerable<string> RequiredFeatureToggles { get; }

        private void CreateTerminalsFromSignature(NIType functionSignature)
        {
            if (functionSignature.IsUnset())
            {
                throw new ArgumentException("Cannot create terminals from an Unset signature", nameof(functionSignature));
            }
            Signature signature = Signatures.GetSignatureForNIType(functionSignature);
            foreach (SignatureTerminal signatureTerminal in signature.Inputs)
            {
                CreateTerminal(Direction.Input, signatureTerminal.DisplayType, signatureTerminal.Name);
            }
            foreach (SignatureTerminal signatureTerminal in signature.Outputs)
            {
                CreateTerminal(Direction.Output, signatureTerminal.DisplayType, signatureTerminal.Name);
            }
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new FunctionalNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitFunctionalNode(this);
        }
    }
}
