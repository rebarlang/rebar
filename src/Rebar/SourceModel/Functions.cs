using System.Collections.Generic;
using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;

namespace Rebar.SourceModel
{
    #region Value manipulation

    public class CreateCopyNode : FunctionalNode
    {
        private const string ElementName = "CreateCopyNode";

        protected CreateCopyNode()
            : base(Signatures.CreateCopyType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static CreateCopyNode CreateCreateCopyNode(IElementCreateInfo elementCreateInfo)
        {
            var createCopyNode = new CreateCopyNode();
            createCopyNode.Init(elementCreateInfo);
            return createCopyNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    /// <summary>
    /// Testing node that prints a value to the debug output window.
    /// </summary>
    public class Output : FunctionalNode
    {
        private const string ElementName = "Output";

        protected Output()
            : base(Signatures.OutputType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Output CreateOutput(IElementCreateInfo elementCreateInfo)
        {
            var outputNode = new Output();
            outputNode.Init(elementCreateInfo);
            return outputNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.OutputNode };
    }

    #endregion

    #region Primitives

    public abstract class PureUnaryPrimitive : FunctionalNode
    {
        protected PureUnaryPrimitive(UnaryPrimitiveOps unaryOp)
            : base(Signatures.DefinePureUnaryFunction(unaryOp.ToString(), unaryOp.GetExpectedInputType(), unaryOp.GetExpectedInputType()))
        {
        }
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

    public abstract class PureBinaryPrimitive : FunctionalNode
    {
        protected PureBinaryPrimitive(BinaryPrimitiveOps binaryOp)
            : base(Signatures.DefinePureBinaryFunction(binaryOp.ToString(), binaryOp.GetExpectedInputType(), binaryOp.GetExpectedInputType()))
        {
        }
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

    public abstract class MutatingUnaryPrimitive : FunctionalNode
    {
        protected MutatingUnaryPrimitive(UnaryPrimitiveOps unaryOp)
            : base(Signatures.DefineMutatingUnaryFunction("Accumulate" + unaryOp.ToString(), unaryOp.GetExpectedInputType()))
        {
        }
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

    public abstract class MutatingBinaryPrimitive : FunctionalNode
    {
        protected MutatingBinaryPrimitive(BinaryPrimitiveOps binaryOp)
            : base(Signatures.DefineMutatingBinaryFunction("Accumulate" + binaryOp.ToString(), binaryOp.GetExpectedInputType()))
        {
        }
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

    #endregion

    #region Iterator

    /// <summary>
    /// Function that takes integer lower (inclusive) and upper (exclusive) bounds and returns an integer iterator.
    /// </summary>
    public class Range : FunctionalNode
    {
        private const string ElementName = "Range";

        protected Range()
            : base(Signatures.RangeType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Range CreateRange(IElementCreateInfo elementCreateInfo)
        {
            var range = new Range();
            range.Init(elementCreateInfo);
            return range;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    #endregion

    #region Vector

    public class VectorCreate : FunctionalNode
    {
        private const string ElementName = "VectorCreate";

        protected VectorCreate()
            : base(Signatures.VectorCreateType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorCreate CreateVectorCreate(IElementCreateInfo elementCreateInfo)
        {
            var vectorCreate = new VectorCreate();
            vectorCreate.Init(elementCreateInfo);
            return vectorCreate;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.VectorAndSliceTypes };

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;
    }

    public class VectorInsert : FunctionalNode
    {
        private const string ElementName = "VectorInsert";

        protected VectorInsert()
            : base(Signatures.VectorInsertType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorInsert CreateVectorInsert(IElementCreateInfo elementCreateInfo)
        {
            var vectorInsert = new VectorInsert();
            vectorInsert.Init(elementCreateInfo);
            return vectorInsert;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    #endregion

    #region Test nodes

    public class ImmutablePassthroughNode : FunctionalNode
    {
        private const string ElementName = "ImmutablePassthroughNode";

        protected ImmutablePassthroughNode()
            : base(Signatures.ImmutablePassthroughType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static ImmutablePassthroughNode CreateImmutablePassthroughNode(IElementCreateInfo elementCreateInfo)
        {
            var immutablePassthroughNode = new ImmutablePassthroughNode();
            immutablePassthroughNode.Init(elementCreateInfo);
            return immutablePassthroughNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class MutablePassthroughNode : FunctionalNode
    {
        private const string ElementName = "MutablePassthroughNode";

        protected MutablePassthroughNode()
            : base(Signatures.MutablePassthroughType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static MutablePassthroughNode CreateMutablePassthroughNode(IElementCreateInfo elementCreateInfo)
        {
            var mutablePassthroughNode = new MutablePassthroughNode();
            mutablePassthroughNode.Init(elementCreateInfo);
            return mutablePassthroughNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    #endregion
}
