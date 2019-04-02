using NationalInstruments.Design;
using NationalInstruments.SourceModel;

namespace Rebar.Design
{
    public class BasicNodeViewModel : NodeViewModel
    {
        private readonly string _foregroundExplicitUri;

        public BasicNodeViewModel(Node node, string name)
            : this(node, name, null)
        {
        }

        public BasicNodeViewModel(Node node, string name, string foregroundExplicitUri)
            : base(node)
        {
            Name = name;
            _foregroundExplicitUri = foregroundExplicitUri;
        }

        /// <inheritoc />
        public override string Name { get; }

        /// <inheritoc />
        protected override ResourceUri ForegroundUri
        {
            get
            {
                if (_foregroundExplicitUri != null)
                {
                    return new ResourceUri(this, _foregroundExplicitUri);
                }
                return base.ForegroundUri;
            }
        }
    }
}
