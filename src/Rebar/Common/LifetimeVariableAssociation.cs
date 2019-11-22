using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using Rebar.Compiler;

namespace Rebar.Common
{
    internal sealed class LifetimeVariableAssociation
    {
        private class LifetimeVariableInfo
        {
            public List<VariableReference> InterruptedVariables { get; } = new List<VariableReference>();
        }

        private enum VariableStatusKind
        {
            Live,

            Interrupted,

            Consumed
        }

        private struct VariableStatus
        {
            private readonly Terminal _terminal;

            private VariableStatus(VariableStatusKind kind, Terminal terminal)
            {
                Kind = kind;
                _terminal = terminal;
            }

            public static VariableStatus Consumed { get; } = new VariableStatus(VariableStatusKind.Consumed, null);

            public static VariableStatus Interrupted { get; } = new VariableStatus(VariableStatusKind.Interrupted, null);

            public static VariableStatus CreateLiveStatus(Terminal terminal) => new VariableStatus(VariableStatusKind.Live, terminal);

            public VariableStatusKind Kind { get; }

            public Terminal Terminal
            {
                get
                {
                    if (Kind == VariableStatusKind.Live)
                    {
                        return _terminal;
                    }
                    throw new InvalidOperationException("Trying to get terminal associated with a non-Live variable");
                }
            }
        }

        private readonly Dictionary<Lifetime, LifetimeVariableInfo> _lifetimeVariableInfos = new Dictionary<Lifetime, LifetimeVariableInfo>();

        private readonly Dictionary<VariableReference, VariableStatus> _variableStatuses = VariableReference.CreateDictionaryWithUniqueVariableKeys<VariableStatus>();

        private LifetimeVariableInfo GetLifetimeVariableInfo(Lifetime lifetime)
        {
            LifetimeVariableInfo info;
            if (!_lifetimeVariableInfos.TryGetValue(lifetime, out info))
            {
                info = new LifetimeVariableInfo();
                _lifetimeVariableInfos[lifetime] = info;
            }
            return info;
        }

        public void AddVariableInterruptedByLifetime(VariableReference variableReference, Lifetime lifetime)
        {
            GetLifetimeVariableInfo(lifetime).InterruptedVariables.Add(variableReference);
        }

        public IEnumerable<VariableReference> GetVariablesInterruptedByLifetime(Lifetime lifetime)
        {
            return GetLifetimeVariableInfo(lifetime).InterruptedVariables;
        }

        public void MarkVariableConsumed(VariableReference variableReference)
        {
            _variableStatuses[variableReference] = VariableStatus.Consumed;
        }

        public void MarkVariableInterrupted(VariableReference variableReference)
        {
            _variableStatuses[variableReference] = VariableStatus.Interrupted;
        }

        public void MarkVariableLive(VariableReference variableReference, Terminal outputTerminal)
        {
            VariableStatus existingStatus;
            if (_variableStatuses.TryGetValue(variableReference, out existingStatus) && existingStatus.Kind == VariableStatusKind.Consumed)
            {
                throw new InvalidOperationException("Trying to mark a variable live that was already marked consumed");
            }
            _variableStatuses[variableReference] = VariableStatus.CreateLiveStatus(outputTerminal);
        }

        public bool IsVariableConsumed(VariableReference variableReference)
        {
            VariableStatus existingStatus;
            return _variableStatuses.TryGetValue(variableReference, out existingStatus) && existingStatus.Kind == VariableStatusKind.Consumed;
        }

        public bool IsVariableInterrupted(VariableReference variableReference)
        {
            VariableStatus existingStatus;
            return _variableStatuses.TryGetValue(variableReference, out existingStatus) && existingStatus.Kind == VariableStatusKind.Interrupted;
        }

        public bool IsLive(VariableReference variableReference)
        {
            VariableStatus existingStatus;
            return _variableStatuses.TryGetValue(variableReference, out existingStatus) && existingStatus.Kind == VariableStatusKind.Live;
        }

        public bool TryGetVariableLiveTerminal(VariableReference variableReference, out Terminal liveTerminal)
        {
            VariableStatus existingStatus;
            if (_variableStatuses.TryGetValue(variableReference, out existingStatus) && existingStatus.Kind == VariableStatusKind.Live)
            {
                liveTerminal = existingStatus.Terminal;
                return true;
            }
            liveTerminal = null;
            return false;
        }

        public bool TryGetBoundedLifetimeWithLiveVariables(out BoundedLifetimeLiveVariableSet boundedLifetimeLiveVariableSet)
        {
            foreach (var kvp in _variableStatuses)
            {
                if (kvp.Value.Kind != VariableStatusKind.Live)
                {
                    continue;
                }
                VariableReference liveVariable = kvp.Key;
                Lifetime variableLifetime = liveVariable.Lifetime;
                if (!variableLifetime.IsBounded)
                {
                    continue;
                }
                VariableSet variableSet = kvp.Value.Terminal.GetVariableSet();
                var variablesInLifetime = variableSet.GetUniqueVariableReferences().Where(v => v.Lifetime == variableLifetime);
                if (variablesInLifetime.Any(IsVariableInterrupted))
                {
                    continue;
                }
                var liveVariablesInLifetime = variablesInLifetime.Where(IsLive);
                LiveVariable[] liveVariables = liveVariablesInLifetime.Select(v => new LiveVariable(v, _variableStatuses[v].Terminal)).ToArray();
                boundedLifetimeLiveVariableSet = new BoundedLifetimeLiveVariableSet(variableLifetime, liveVariables);
                return true;
            }
            boundedLifetimeLiveVariableSet = null;
            return false;
        }

        public bool TryGetLiveVariableWithUnboundedLifetime(out LiveVariable liveVariable)
        {
            foreach (var kvp in _variableStatuses)
            {
                if (kvp.Value.Kind == VariableStatusKind.Live && !kvp.Key.Lifetime.IsBounded)
                {
                    liveVariable = new LiveVariable(kvp.Key, kvp.Value.Terminal);
                    return true;
                }
            }
            liveVariable = new LiveVariable();
            return false;
        }
    }

    internal class BoundedLifetimeLiveVariableSet
    {
        public BoundedLifetimeLiveVariableSet(Lifetime lifetime, LiveVariable[] liveVariables)
        {
            Lifetime = lifetime;
            LiveVariables = liveVariables;
        }

        public Lifetime Lifetime { get; }

        public IEnumerable<LiveVariable> LiveVariables { get; }
    }

    internal struct LiveVariable
    {
        public LiveVariable(VariableReference variable, Terminal terminal)
        {
            Variable = variable;
            Terminal = terminal;
        }

        public VariableReference Variable { get; }

        public Terminal Terminal { get; }

        public void ConnectToTerminalAsInputAndUnifyVariables(Terminal connectTo, ITypeUnificationResultFactory unificationResultFactory)
        {
            Wire.Create(Terminal.ParentDiagram, Terminal, connectTo);
            AutoBorrowNodeFacade.GetNodeFacade(connectTo.ParentNode)[connectTo]
                .UnifyWithConnectedWireTypeAsNodeInput(Variable, unificationResultFactory);
        }
    }
}
