using System.Collections.Generic;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public abstract class SimpleNode : Node
    {
        public static readonly PropertySymbol NodeTerminalsPropertySymbol =
            ExposeFixedNodeTerminalsProperty<SimpleNode>(PropertySerializers.NodeTerminalsConnectedFixedReferenceSerializer);

        protected SimpleNode()
        {
            FixedTerminals = new OwnerComponentCollection(this);
        }

        protected OwnerComponentCollection FixedTerminals { get; }

        public override IEnumerable<Element> Components => FixedTerminals;

        /// <inheritdoc />
        protected override void Init(IElementCreateInfo info)
        {
            base.Init(info);
            SetIconViewGeometry();
        }

        /// <inheritdoc />
        public override void EnsureView(EnsureViewHints hints)
        {
            if (hints.HasTemplateHint())
            {
                if (Template == ViewElementTemplate.Icon)
                {
                    SetIconViewGeometry();
                }
                else
                {
                    ArrangeListView();
                }
            }
            else if (hints.HasBoundsHint())
            {
                ArrangeListViewOutputs();
            }
        }

        protected abstract void SetIconViewGeometry();
    }
}
