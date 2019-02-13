using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Dfir;

namespace Rebar.Compiler
{
    internal class PropagateTypePermissivenessTransform : IDfirTransform
    {
        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            // visit DfirRoot nodes in execution order, update one-to-one output reference terminals with type permissiveness from input terminal
        }
    }
}
