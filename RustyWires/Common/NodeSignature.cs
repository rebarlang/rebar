using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Compiler;

namespace RustyWires.Common
{
    internal class NodeSignature
    {
        public NodeSignature(string signatureName)
        {
            SignatureName = signatureName;
        }

        public string SignatureName { get; }

        public List<Parameter> Parameters { get; } = new List<Parameter>();

        public void PropagateTypes(Node dfirNode)
        {
            // check that required terminals are wired
            dfirNode.PullInputTypes();
            List<Tuple<Terminal, NIType, Lifetime>> inputParameterInfo = new List<Tuple<Terminal, NIType, Lifetime>>();
            foreach (var inputTerminal in dfirNode.InputTerminals)
            {
                if (inputTerminal.TestRequiredTerminalConnected())
                {
                    inputParameterInfo.Add(new Tuple<Terminal, NIType, Lifetime>(inputTerminal, inputTerminal.DataType, inputTerminal.GetSourceLifetime()));
                }
                else
                {
                    inputParameterInfo.Add(new Tuple<Terminal, NIType, Lifetime>(inputTerminal, PFTypes.Void.CreateImmutableReference(), Lifetime.Empty));
                }
            }

            Dictionary<string, NIType> genericTypeArguments = new Dictionary<string, NIType>();
            Dictionary<string, Lifetime> lifetimeArguments = new Dictionary<string, Lifetime>();

            List<Parameter> inputParameters = new List<Parameter>();
            inputParameters.AddRange(Parameters.Where(p => p.Direction == Direction.Bidirectional));
            inputParameters.AddRange(Parameters.Where(p => p.Direction == Direction.Input));
            // assert that inputParameterInfo and inputParameters have same count
            foreach (var pair in inputParameterInfo.Zip(inputParameters))
            {
                var terminal = pair.Key.Item1;
                var formalParameter = pair.Value;
                var actualType = pair.Key.Item2;
                var actualLifetime = pair.Key.Item3;
                var formalParameterPermission = formalParameter.Type.Permission;
                // check that connected wires match type permissions
                if (formalParameterPermission == TypePermissiveness.MutableReference ||
                    formalParameterPermission == TypePermissiveness.MutableOwner)
                {
                    // TODO: we already know the terminal type; maybe we should just build up a
                    // list of messages to add to the terminal
                    terminal.TestTerminalHasMutableTypeConnected();
                }

                if (formalParameterPermission == TypePermissiveness.Owner ||
                    formalParameterPermission == TypePermissiveness.MutableOwner)
                {
                    terminal.TestTerminalHasOwnedValueConnected();
                }

                // check that connected references wires have non-empty lifetime
                if (actualType.IsRWReferenceType() && actualLifetime.IsEmpty && pair.Key.Item1.IsConnected)
                {
                    terminal.SetDfirMessage(RustyWiresMessages.WiredReferenceDoesNotLiveLongEnough);
                }

                // compute generic type arguments and check compatibility
                // TODO
                NIType underlyingType = actualType.GetUnderlyingTypeFromRustyWiresType();
                if (formalParameter.Type.GenericTypeParameter != null)
                {
                    genericTypeArguments[formalParameter.Type.GenericTypeParameter] = underlyingType;
                }
                else
                {
                    throw new NotImplementedException("Don't know how to handle a non-generic parameter type");
                }

                // compute lifetime arguments
                // TODO
                Lifetime effectiveLifetime = terminal.ComputeInputTerminalEffectiveLifetime();
                if (formalParameter.Type.LifetimeParameter != null)
                {
                    lifetimeArguments[formalParameter.Type.LifetimeParameter] = effectiveLifetime;
                }
            }

            // set output types and lifetimes
            List<Parameter> outputParameters = new List<Parameter>();
            outputParameters.AddRange(Parameters.Where(p => p.Direction == Direction.Bidirectional));
            outputParameters.AddRange(Parameters.Where(p => p.Direction == Direction.Output));
            int index = 0;
            foreach (var pair in dfirNode.OutputTerminals.Zip(outputParameters))
            {
                var terminal = pair.Key;
                var formalParameter = pair.Value;
                NIType dataType = PFTypes.Void;
                if (formalParameter.Type.GenericTypeParameter != null)
                {
                    NIType genericTypeArgument;
                    if (genericTypeArguments.TryGetValue(formalParameter.Type.GenericTypeParameter, out genericTypeArgument))
                    {
                        dataType = genericTypeArgument;
                    }
                }

                TypePermissiveness outputPermission = formalParameter.Type.Permission;
                if (formalParameter.Direction == Direction.Bidirectional)
                {
                    outputPermission = inputParameterInfo[index].Item2.GetTypePermissiveness();
                }

                Lifetime outputLifetime = Lifetime.Empty;
                if (formalParameter.Direction == Direction.Bidirectional)
                {
                    outputLifetime = inputParameterInfo[index].Item3;
                }
                else if (formalParameter.Type.LifetimeParameter != null)
                {
                    if (!lifetimeArguments.TryGetValue(formalParameter.Type.LifetimeParameter, out outputLifetime))
                    {
                        outputLifetime = Lifetime.Empty;
                    }
                }

                Variable terminalVariable = terminal.GetVariable();
                switch (outputPermission)
                {
                    case TypePermissiveness.ImmutableReference:
                        terminalVariable.SetTypeAndLifetime(dataType.CreateImmutableReference(), outputLifetime);
                        break;
                    case TypePermissiveness.MutableReference:
                        terminalVariable.SetTypeAndLifetime(dataType.CreateMutableReference(), outputLifetime);
                        break;
                    case TypePermissiveness.Owner:
                    case TypePermissiveness.MutableOwner:
                        terminalVariable.SetTypeAndLifetime(dataType, Lifetime.Unbounded);
                        break;
                }
                ++index;
            }
        }
    }

    internal class Parameter
    {
        public Direction Direction { get; set; }
        public TypeDescriptor Type { get; set; }
    }

    internal class TypeDescriptor
    {
        public TypePermissiveness Permission { get; set; }
        public NIType DataType { get; set; }
        public string GenericTypeParameter { get; set; }
        public string LifetimeParameter { get; set; }
    }

    internal class NodeSignatureBuilder
    {
        private readonly List<Parameter> _parameters = new List<Parameter>();

        public NodeSignatureBuilder(string signatureName)
        {
            SignatureName = signatureName;
        }

        public string SignatureName { get; }

        public NodeSignature CreateNodeSignature()
        {
            var nodeSignature = new NodeSignature(SignatureName);
            nodeSignature.Parameters.AddRange(_parameters);
            return nodeSignature;
        }

        public GenericTypeParameterBuilder DefineGenericTypeParameter(string parameterName)
        {
            var genericTypeParameter = new GenericTypeParameterBuilder(parameterName);
            return genericTypeParameter;
        }

        public void DefinePassthroughParameter(bool mutable, GenericTypeParameterBuilder parameterGenericType)
        {
            _parameters.Add(new Parameter()
            {
                Direction = Direction.Bidirectional,
                Type = new TypeDescriptor()
                {
                    GenericTypeParameter = parameterGenericType.Name,
                    LifetimeParameter = "L1", // TODO
                    Permission = mutable ? TypePermissiveness.MutableReference : TypePermissiveness.ImmutableReference
                }
            });
        }
    }

    internal class GenericTypeParameterBuilder
    {
        public GenericTypeParameterBuilder(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
