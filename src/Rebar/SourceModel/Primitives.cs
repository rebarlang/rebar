using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Compiler;
using Rebar.Common;

namespace Rebar.SourceModel
{
    public abstract class PureBinaryPrimitive : SimpleNode
    {
        protected PureBinaryPrimitive(BinaryPrimitiveOps binaryOp)
        {
            Operation = binaryOp;
            NIType inputType = binaryOp.GetExpectedInputType();
            NIType inputReferenceType = inputType.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, inputReferenceType, "x in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, inputReferenceType, "y in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, inputReferenceType, "x out"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, inputReferenceType, "y out"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, inputType, "result"));
        }

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 6);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 3);
            terminals[2].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
            terminals[3].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 3);
            terminals[4].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 5);
        }

        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitPureBinaryPrimitive(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        public BinaryPrimitiveOps Operation { get; }
    }

    public abstract class PureUnaryPrimitive : SimpleNode
    {
        protected PureUnaryPrimitive(UnaryPrimitiveOps unaryOp)
        {
            Operation = unaryOp;
            NIType inputType = unaryOp.GetExpectedInputType();
            NIType inputReferenceType = inputType.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, inputReferenceType, "x in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, inputReferenceType, "x out"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, inputType, "result"));
        }

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
            terminals[2].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 3);
        }

        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitPureUnaryPrimitive(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        public UnaryPrimitiveOps Operation { get; }
    }

    public abstract class MutatingBinaryPrimitive : SimpleNode
    {
        protected MutatingBinaryPrimitive(BinaryPrimitiveOps binaryOp)
        {
            Operation = binaryOp;
            NIType inputType = binaryOp.GetExpectedInputType();
            NIType inputMutableReferenceType = inputType.CreateMutableReference();
            NIType inputImmutableReferenceType = inputType.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, inputMutableReferenceType, "x in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, inputImmutableReferenceType, "y in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, inputMutableReferenceType, "x out"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, inputImmutableReferenceType, "y out"));
        }

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 3);
            terminals[2].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
            terminals[3].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 3);
        }

        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitMutatingBinaryPrimitive(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        public BinaryPrimitiveOps Operation { get; }
    }

    public abstract class MutatingUnaryPrimitive : SimpleNode
    {
        protected MutatingUnaryPrimitive(UnaryPrimitiveOps unaryOp)
        {
            Operation = unaryOp;
            NIType inputType = unaryOp.GetExpectedInputType();
            NIType intMutableReferenceType = inputType.CreateMutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, intMutableReferenceType, "x in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, intMutableReferenceType, "x out"));
        }

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitMutatingUnaryPrimitive(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        public UnaryPrimitiveOps Operation { get; }
    }

    public class Add : PureBinaryPrimitive
    {
        public Add() : base(BinaryPrimitiveOps.Add) { }

        private const string ElementName = "Add";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Add CreateAdd(IElementCreateInfo elementCreateInfo)
        {
            var add = new Add();
            add.Init(elementCreateInfo);
            return add;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Subtract : PureBinaryPrimitive
    {
        public Subtract() : base(BinaryPrimitiveOps.Subtract) { }

        private const string ElementName = "Subtract";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Subtract CreateSubtract(IElementCreateInfo elementCreateInfo)
        {
            var subtract = new Subtract();
            subtract.Init(elementCreateInfo);
            return subtract;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Multiply : PureBinaryPrimitive
    {
        public Multiply() : base(BinaryPrimitiveOps.Multiply) { }

        private const string ElementName = "Multiply";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Multiply CreateMultiply(IElementCreateInfo elementCreateInfo)
        {
            var multiply = new Multiply();
            multiply.Init(elementCreateInfo);
            return multiply;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Divide : PureBinaryPrimitive
    {
        public Divide() : base(BinaryPrimitiveOps.Divide) { }

        private const string ElementName = "Divide";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Divide CreateDivide(IElementCreateInfo elementCreateInfo)
        {
            var divide = new Divide();
            divide.Init(elementCreateInfo);
            return divide;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class And : PureBinaryPrimitive
    {
        public And() : base(BinaryPrimitiveOps.And) { }

        private const string ElementName = "And";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static And CreateAdd(IElementCreateInfo elementCreateInfo)
        {
            var and = new And();
            and.Init(elementCreateInfo);
            return and;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Or : PureBinaryPrimitive
    {
        public Or() : base(BinaryPrimitiveOps.Or) { }

        private const string ElementName = "Or";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Or CreateOr(IElementCreateInfo elementCreateInfo)
        {
            var or = new Or();
            or.Init(elementCreateInfo);
            return or;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Xor : PureBinaryPrimitive
    {
        public Xor() : base(BinaryPrimitiveOps.Xor) { }

        private const string ElementName = "Xor";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Xor CreateXor(IElementCreateInfo elementCreateInfo)
        {
            var xor = new Xor();
            xor.Init(elementCreateInfo);
            return xor;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Increment : PureUnaryPrimitive
    {
        public Increment() : base(UnaryPrimitiveOps.Increment) { }

        private const string ElementName = "Increment";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Increment CreateIncrement(IElementCreateInfo elementCreateInfo)
        {
            var increment = new Increment();
            increment.Init(elementCreateInfo);
            return increment;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Not : PureUnaryPrimitive
    {
        public Not() : base(UnaryPrimitiveOps.Not) { }

        private const string ElementName = "Not";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Not CreateIncrement(IElementCreateInfo elementCreateInfo)
        {
            var not = new Not();
            not.Init(elementCreateInfo);
            return not;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateAdd : MutatingBinaryPrimitive
    {
        public AccumulateAdd() : base(BinaryPrimitiveOps.Add) { }

        private const string ElementName = "AccumulateAdd";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateAdd CreateAccumulateAdd(IElementCreateInfo elementCreateInfo)
        {
            var accumulateAdd = new AccumulateAdd();
            accumulateAdd.Init(elementCreateInfo);
            return accumulateAdd;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateSubtract : MutatingBinaryPrimitive
    {
        public AccumulateSubtract() : base(BinaryPrimitiveOps.Subtract) { }

        private const string ElementName = "AccumulateSubtract";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateSubtract CreateAccumulateSubtract(IElementCreateInfo elementCreateInfo)
        {
            var accumulateSubtract = new AccumulateSubtract();
            accumulateSubtract.Init(elementCreateInfo);
            return accumulateSubtract;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateMultiply : MutatingBinaryPrimitive
    {
        public AccumulateMultiply() : base(BinaryPrimitiveOps.Multiply) { }

        private const string ElementName = "AccumulateMultiply";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateMultiply CreateAccumulateMultiply(IElementCreateInfo elementCreateInfo)
        {
            var accumulateMultiply = new AccumulateMultiply();
            accumulateMultiply.Init(elementCreateInfo);
            return accumulateMultiply;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateDivide : MutatingBinaryPrimitive
    {
        public AccumulateDivide() : base(BinaryPrimitiveOps.Divide) { }

        private const string ElementName = "AccumulateDivide";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateDivide CreateAccumulateAdd(IElementCreateInfo elementCreateInfo)
        {
            var accumulateDivide = new AccumulateDivide();
            accumulateDivide.Init(elementCreateInfo);
            return accumulateDivide;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateAnd : MutatingBinaryPrimitive
    {
        public AccumulateAnd() : base(BinaryPrimitiveOps.And) { }

        private const string ElementName = "AccumulateAnd";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateAnd CreateAccumulateAnd(IElementCreateInfo elementCreateInfo)
        {
            var accumulateAnd = new AccumulateAnd();
            accumulateAnd.Init(elementCreateInfo);
            return accumulateAnd;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateOr : MutatingBinaryPrimitive
    {
        public AccumulateOr() : base(BinaryPrimitiveOps.Or) { }

        private const string ElementName = "AccumulateOr";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateOr CreateAccumulateOr(IElementCreateInfo elementCreateInfo)
        {
            var accumulateOr = new AccumulateOr();
            accumulateOr.Init(elementCreateInfo);
            return accumulateOr;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateXor : MutatingBinaryPrimitive
    {
        public AccumulateXor() : base(BinaryPrimitiveOps.Xor) { }

        private const string ElementName = "AccumulateXor";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateXor CreateAccumulateXor(IElementCreateInfo elementCreateInfo)
        {
            var accumulateXor = new AccumulateXor();
            accumulateXor.Init(elementCreateInfo);
            return accumulateXor;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateIncrement : MutatingUnaryPrimitive
    {
        public AccumulateIncrement() : base(UnaryPrimitiveOps.Increment) { }

        private const string ElementName = "AccumulateIncrement";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateIncrement CreateAccumulateIncrement(IElementCreateInfo elementCreateInfo)
        {
            var accumulateIncrement = new AccumulateIncrement();
            accumulateIncrement.Init(elementCreateInfo);
            return accumulateIncrement;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class AccumulateNot : MutatingUnaryPrimitive
    {
        public AccumulateNot() : base(UnaryPrimitiveOps.Not) { }

        private const string ElementName = "AccumulateNot";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateNot CreateIncrement(IElementCreateInfo elementCreateInfo)
        {
            var accumulateNot = new AccumulateNot();
            accumulateNot.Init(elementCreateInfo);
            return accumulateNot;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}