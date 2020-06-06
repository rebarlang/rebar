using System.Collections.Generic;
using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;

namespace Rebar.SourceModel
{
    #region Value manipulation

    /// <summary>
    /// Node that consumes one variable and assigns its value into another, whose old value is dropped.
    /// </summary>
    public class AssignNode : FunctionalNode
    {
        private const string ElementName = "AssignNode";

        protected AssignNode()
            : base(Signatures.AssignType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AssignNode CreateAssignNode(IElementCreateInfo elementCreateInfo)
        {
            var assignNode = new AssignNode();
            assignNode.Initialize(elementCreateInfo);
            return assignNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class ExchangeValues : FunctionalNode
    {
        private const string ElementName = "ExchangeValues";

        protected ExchangeValues()
            : base(Signatures.ExchangeValuesType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static ExchangeValues CreateExchangeValues(IElementCreateInfo elementCreateInfo)
        {
            var exchangeValues = new ExchangeValues();
            exchangeValues.Initialize(elementCreateInfo);
            return exchangeValues;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

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
            createCopyNode.Initialize(elementCreateInfo);
            return createCopyNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class SelectReferenceNode : FunctionalNode
    {
        private const string ElementName = "SelectReferenceNode";

        protected SelectReferenceNode()
            : base(Signatures.SelectReferenceType)
        {
            Width = StockDiagramGeometries.GridSize * 8;
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SelectReferenceNode CreateSelectReferenceNode(IElementCreateInfo elementCreateInfo)
        {
            var selectReferenceNode = new SelectReferenceNode();
            selectReferenceNode.Initialize(elementCreateInfo);
            return selectReferenceNode;
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
            outputNode.Initialize(elementCreateInfo);
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
            increment.Initialize(elementCreateInfo);
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
            not.Initialize(elementCreateInfo);
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
            add.Initialize(elementCreateInfo);
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
            subtract.Initialize(elementCreateInfo);
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
            multiply.Initialize(elementCreateInfo);
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
            divide.Initialize(elementCreateInfo);
            return divide;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Modulus : PureBinaryPrimitive
    {
        public Modulus() : base(BinaryPrimitiveOps.Modulus) { }

        private const string ElementName = "Modulus";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Modulus CreateModulus(IElementCreateInfo elementCreateInfo)
        {
            var modulus = new Modulus();
            modulus.Initialize(elementCreateInfo);
            return modulus;
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
            and.Initialize(elementCreateInfo);
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
            or.Initialize(elementCreateInfo);
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
            xor.Initialize(elementCreateInfo);
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

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;
    }

    public class AccumulateIncrement : MutatingUnaryPrimitive
    {
        public AccumulateIncrement() : base(UnaryPrimitiveOps.Increment) { }

        private const string ElementName = "AccumulateIncrement";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AccumulateIncrement CreateAccumulateIncrement(IElementCreateInfo elementCreateInfo)
        {
            var accumulateIncrement = new AccumulateIncrement();
            accumulateIncrement.Initialize(elementCreateInfo);
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
            accumulateNot.Initialize(elementCreateInfo);
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
            accumulateAdd.Initialize(elementCreateInfo);
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
            accumulateSubtract.Initialize(elementCreateInfo);
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
            accumulateMultiply.Initialize(elementCreateInfo);
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
            accumulateDivide.Initialize(elementCreateInfo);
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
            accumulateAnd.Initialize(elementCreateInfo);
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
            accumulateOr.Initialize(elementCreateInfo);
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
            accumulateXor.Initialize(elementCreateInfo);
            return accumulateXor;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class Equal : FunctionalNode
    {
        private const string ElementName = "Equal";

        public Equal() : base(Signatures.DefineComparisonFunction(ElementName)) { }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Equal CreateEqual(IElementCreateInfo elementCreateInfo)
        {
            var equal = new Equal();
            equal.Initialize(elementCreateInfo);
            return equal;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class NotEqual : FunctionalNode
    {
        private const string ElementName = "NotEqual";

        public NotEqual() : base(Signatures.DefineComparisonFunction(ElementName)) { }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static NotEqual CreateNotEqual(IElementCreateInfo elementCreateInfo)
        {
            var notEqual = new NotEqual();
            notEqual.Initialize(elementCreateInfo);
            return notEqual;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class LessThan : FunctionalNode
    {
        private const string ElementName = "LessThan";

        public LessThan() : base(Signatures.DefineComparisonFunction(ElementName)) { }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static LessThan CreateLessThan(IElementCreateInfo elementCreateInfo)
        {
            var lessThan = new LessThan();
            lessThan.Initialize(elementCreateInfo);
            return lessThan;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class LessEqual : FunctionalNode
    {
        private const string ElementName = "LessEqual";

        public LessEqual() : base(Signatures.DefineComparisonFunction(ElementName)) { }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static LessEqual CreateLessEqual(IElementCreateInfo elementCreateInfo)
        {
            var lessEqual = new LessEqual();
            lessEqual.Initialize(elementCreateInfo);
            return lessEqual;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class GreaterThan : FunctionalNode
    {
        private const string ElementName = "GreaterThan";

        public GreaterThan() : base(Signatures.DefineComparisonFunction(ElementName)) { }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static GreaterThan CreateGreaterThan(IElementCreateInfo elementCreateInfo)
        {
            var greaterThan = new GreaterThan();
            greaterThan.Initialize(elementCreateInfo);
            return greaterThan;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    public class GreaterEqual : FunctionalNode
    {
        private const string ElementName = "GreaterEqual";

        public GreaterEqual() : base(Signatures.DefineComparisonFunction(ElementName)) { }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static GreaterEqual CreateGreaterEqual(IElementCreateInfo elementCreateInfo)
        {
            var greaterEqual = new GreaterEqual();
            greaterEqual.Initialize(elementCreateInfo);
            return greaterEqual;
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
            range.Initialize(elementCreateInfo);
            return range;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    #endregion

    #region Option

    /// <summary>
    /// Node that constructs a Some(x) value from an input value x; the output type is Option&lt;T&gt; 
    /// when the input type is T.
    /// </summary>
    public class SomeConstructorNode : FunctionalNode
    {
        private const string ElementName = "SomeConstructor";

        protected SomeConstructorNode()
            : base(Signatures.SomeConstructorType)
        {
            Width = StockDiagramGeometries.GridSize * 8;
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SomeConstructorNode CreateSomeConstructorNode(IElementCreateInfo elementCreateInfo)
        {
            var someConstructor = new SomeConstructorNode();
            someConstructor.Initialize(elementCreateInfo);
            return someConstructor;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;
    }

    /// <summary>
    /// Node that constructs a None value; the output type is Option&lt;T&gt; for some T determined by the downstream usage.
    /// </summary>
    public class NoneConstructorNode : FunctionalNode
    {
        private const string ElementName = "NoneConstructor";

        protected NoneConstructorNode()
            : base(Signatures.NoneConstructorType)
        {
            Width = StockDiagramGeometries.GridSize * 8;
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static NoneConstructorNode CreateNoneConstructorNode(IElementCreateInfo elementCreateInfo)
        {
            var noneConstructor = new NoneConstructorNode();
            noneConstructor.Initialize(elementCreateInfo);
            return noneConstructor;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;
    }


    /// <summary>
    /// Node that unwraps a Some(x) into x and panics on None.
    /// </summary>
    public class UnwrapOption : FunctionalNode
    {
        private const string ElementName = "UnwrapOption";

        protected UnwrapOption()
            : base(Signatures.UnwrapOptionType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static UnwrapOption CreateUnwrapOption(IElementCreateInfo elementCreateInfo)
        {
            var unwrapOption = new UnwrapOption();
            unwrapOption.Initialize(elementCreateInfo);
            return unwrapOption;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[] { RebarFeatureToggles.Panics };
    }

    #endregion

    #region String

    public class StringFromSlice : FunctionalNode
    {
        private const string ElementName = "StringFromSlice";

        protected StringFromSlice()
            : base(Signatures.StringFromSliceType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StringFromSlice CreateStringFromSlice(IElementCreateInfo elementCreateInfo)
        {
            var stringFromSlice = new StringFromSlice();
            stringFromSlice.Initialize(elementCreateInfo);
            return stringFromSlice;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.StringDataType };
    }

    public class StringFromByteSlice : FunctionalNode
    {
        private const string ElementName = "StringFromByteSlice";

        protected StringFromByteSlice()
            : base(Signatures.StringFromByteSliceType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StringFromByteSlice CreateStringFromByteSlice(IElementCreateInfo elementCreateInfo)
        {
            var stringFromByteSlice = new StringFromByteSlice();
            stringFromByteSlice.Initialize(elementCreateInfo);
            return stringFromByteSlice;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] 
        {
            RebarFeatureToggles.StringDataType,
            RebarFeatureToggles.VectorAndSliceTypes,
            RebarFeatureToggles.AllIntegerTypes
        };
    }

    public class StringToSlice : FunctionalNode
    {
        private const string ElementName = "StringToSlice";

        protected StringToSlice()
            : base(Signatures.StringToSliceType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StringToSlice CreateStringToSlice(IElementCreateInfo elementCreateInfo)
        {
            var stringToSlice = new StringToSlice();
            stringToSlice.Initialize(elementCreateInfo);
            return stringToSlice;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.StringDataType };
    }

    public class StringConcat : FunctionalNode
    {
        private const string ElementName = "StringConcat";

        protected StringConcat()
            : base(Signatures.StringConcatType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StringConcat CreateStringConcat(IElementCreateInfo elementCreateInfo)
        {
            var stringConcat = new StringConcat();
            stringConcat.Initialize(elementCreateInfo);
            return stringConcat;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.StringDataType };
    }

    public class StringAppend : FunctionalNode
    {
        private const string ElementName = "StringAppend";

        protected StringAppend()
            : base(Signatures.StringAppendType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StringAppend CreateStringAppend(IElementCreateInfo elementCreateInfo)
        {
            var stringAppend = new StringAppend();
            stringAppend.Initialize(elementCreateInfo);
            return stringAppend;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.StringDataType };
    }

    public class StringSliceToStringSplitIterator : FunctionalNode
    {
        private const string ElementName = "StringSliceToStringSplitIterator";

        protected StringSliceToStringSplitIterator()
            : base(Signatures.StringSliceToStringSplitIteratorType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static StringSliceToStringSplitIterator CreateStringSliceToStringSplitIterator(IElementCreateInfo elementCreateInfo)
        {
            var stringSliceToStringSplitIterator = new StringSliceToStringSplitIterator();
            stringSliceToStringSplitIterator.Initialize(elementCreateInfo);
            return stringSliceToStringSplitIterator;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.StringDataType };
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
            vectorCreate.Initialize(elementCreateInfo);
            return vectorCreate;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.VectorAndSliceTypes };

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;
    }

    public class VectorAppend : FunctionalNode
    {
        private const string ElementName = "VectorAppend";

        protected VectorAppend()
            : base(Signatures.VectorAppendType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorAppend CreateVectorAppend(IElementCreateInfo elementCreateInfo)
        {
            var vectorAppend = new VectorAppend();
            vectorAppend.Initialize(elementCreateInfo);
            return vectorAppend;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
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
            vectorInsert.Initialize(elementCreateInfo);
            return vectorInsert;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    public class VectorInitialize : FunctionalNode
    {
        private const string ElementName = "VectorInitialize";

        protected VectorInitialize()
            : base(Signatures.VectorInitializeType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorInitialize CreateVectorInitialize(IElementCreateInfo elementCreateInfo)
        {
            var vectorInitialize = new VectorInitialize();
            vectorInitialize.Initialize(elementCreateInfo);
            return vectorInitialize;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    public class VectorRemoveLast : FunctionalNode
    {
        private const string ElementName = "VectorRemoveLast";

        protected VectorRemoveLast()
            : base(Signatures.VectorRemoveLastType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorRemoveLast CreateVectorRemoveLast(IElementCreateInfo elementCreateInfo)
        {
            var vectorRemoveLast = new VectorRemoveLast();
            vectorRemoveLast.Initialize(elementCreateInfo);
            return vectorRemoveLast;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    public class VectorToSlice : FunctionalNode
    {
        private const string ElementName = "VectorToSlice";

        protected VectorToSlice()
            : base(Signatures.VectorToSliceType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VectorToSlice CreateVectorToSlice(IElementCreateInfo elementCreateInfo)
        {
            var vectorToSlice = new VectorToSlice();
            vectorToSlice.Initialize(elementCreateInfo);
            return vectorToSlice;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    public class SliceIndex : FunctionalNode
    {
        private const string ElementName = "SliceIndex";

        protected SliceIndex()
            : base(Signatures.SliceIndexType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SliceIndex CreateSliceIndex(IElementCreateInfo elementCreateInfo)
        {
            var sliceIndex = new SliceIndex();
            sliceIndex.Initialize(elementCreateInfo);
            return sliceIndex;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    public class SliceToIterator : FunctionalNode
    {
        private const string ElementName = "SliceToIterator";

        protected SliceToIterator()
            : base(Signatures.SliceToIteratorType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SliceToIterator CreateSliceToIterator(IElementCreateInfo elementCreateInfo)
        {
            var sliceToIterator = new SliceToIterator();
            sliceToIterator.Initialize(elementCreateInfo);
            return sliceToIterator;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    public class SliceToMutableIterator : FunctionalNode
    {
        private const string ElementName = "SliceToMutableIterator";

        protected SliceToMutableIterator()
            : base(Signatures.SliceToMutableIteratorType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SliceToMutableIterator CreateSliceToMutableIterator(IElementCreateInfo elementCreateInfo)
        {
            var sliceToMutableIterator = new SliceToMutableIterator();
            sliceToMutableIterator.Initialize(elementCreateInfo);
            return sliceToMutableIterator;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[1] { RebarFeatureToggles.VectorAndSliceTypes };
    }

    #endregion

    #region Cell

    public class CreateLockingCell : FunctionalNode
    {
        private const string ElementName = "CreateLockingCell";

        protected CreateLockingCell()
            : base(Signatures.CreateLockingCellType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static CreateLockingCell CreateCreateLockingCell(IElementCreateInfo elementCreateInfo)
        {
            var createLockingCell = new CreateLockingCell();
            createLockingCell.Initialize(elementCreateInfo);
            return createLockingCell;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.CellDataType };
    }

    public class SharedCreate : FunctionalNode
    {
        private const string ElementName = "SharedCreate";

        protected SharedCreate()
            : base(Signatures.SharedCreateType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SharedCreate CreateSharedCreate(IElementCreateInfo elementCreateInfo)
        {
            var sharedCreate = new SharedCreate();
            sharedCreate.Initialize(elementCreateInfo);
            return sharedCreate;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.CellDataType };
    }

    public class SharedGetValue : FunctionalNode
    {
        private const string ElementName = "SharedGetValue";

        protected SharedGetValue()
            : base(Signatures.SharedGetValueType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SharedGetValue CreateSharedGetValue(IElementCreateInfo elementCreateInfo)
        {
            var sharedGetValue = new SharedGetValue();
            sharedGetValue.Initialize(elementCreateInfo);
            return sharedGetValue;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.CellDataType };
    }

    #endregion

    #region FileHandle

    public class OpenFileHandle : FunctionalNode
    {
        private const string ElementName = "OpenFileHandle";

        protected OpenFileHandle()
            : base(Signatures.OpenFileHandleType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OpenFileHandle CreateOpenFileHandle(IElementCreateInfo elementCreateInfo)
        {
            var openFileHandle = new OpenFileHandle();
            openFileHandle.Initialize(elementCreateInfo);
            return openFileHandle;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.FileHandleDataType, RebarFeatureToggles.StringDataType };
    }

    public class ReadLineFromFileHandle : FunctionalNode
    {
        private const string ElementName = "ReadLineFromFileHandle";

        protected ReadLineFromFileHandle()
            : base(Signatures.ReadLineFromFileHandleType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static ReadLineFromFileHandle CreateReadLineFromFileHandle(IElementCreateInfo elementCreateInfo)
        {
            var readLineFromFileHandle = new ReadLineFromFileHandle();
            readLineFromFileHandle.Initialize(elementCreateInfo);
            return readLineFromFileHandle;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.FileHandleDataType, RebarFeatureToggles.StringDataType };
    }

    public class WriteStringToFileHandle : FunctionalNode
    {
        private const string ElementName = "WriteStringToFileHandle";

        protected WriteStringToFileHandle()
            : base(Signatures.WriteStringToFileHandleType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static WriteStringToFileHandle CreateWriteStringToFileHandle(IElementCreateInfo elementCreateInfo)
        {
            var writeStringToFileHandle = new WriteStringToFileHandle();
            writeStringToFileHandle.Initialize(elementCreateInfo);
            return writeStringToFileHandle;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new[] { RebarFeatureToggles.FileHandleDataType, RebarFeatureToggles.StringDataType };
    }

    #endregion

    #region Async

    public class Yield : FunctionalNode
    {
        private const string ElementName = "Yield";

        protected Yield()
            : base(Signatures.YieldType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static Yield CreateYield(IElementCreateInfo elementCreateInfo)
        {
            var yield = new Yield();
            yield.Initialize(elementCreateInfo);
            return yield;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;
    }

    #endregion

    #region Notifier

    public class NotifierCreate : FunctionalNode
    {
        private const string ElementName = "NotifierCreate";

        protected NotifierCreate()
            : base(Signatures.CreateNotifierPairType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static NotifierCreate CreateNotifierCreate(IElementCreateInfo elementCreateInfo)
        {
            var notifierCreate = new NotifierCreate();
            notifierCreate.Initialize(elementCreateInfo);
            return notifierCreate;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[] { RebarFeatureToggles.NotifierType };
    }

    public class SetNotifierValue : FunctionalNode
    {
        private const string ElementName = "SetNotifierValue";

        protected SetNotifierValue()
            : base(Signatures.SetNotifierValueType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SetNotifierValue CreateSetNotifierValue(IElementCreateInfo elementCreateInfo)
        {
            var setNotifierValue = new SetNotifierValue();
            setNotifierValue.Initialize(elementCreateInfo);
            return setNotifierValue;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[] { RebarFeatureToggles.NotifierType };
    }

    public class GetNotifierValue : FunctionalNode
    {
        private const string ElementName = "GetNotifierValue";

        protected GetNotifierValue()
            : base(Signatures.GetNotifierValueType)
        {
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static GetNotifierValue CreateGetNotifierValue(IElementCreateInfo elementCreateInfo)
        {
            var getNotifierValue = new GetNotifierValue();
            getNotifierValue.Initialize(elementCreateInfo);
            return getNotifierValue;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override float MinimumHeight => StockDiagramGeometries.GridSize * 4;

        /// <inheritdoc />
        public override IEnumerable<string> RequiredFeatureToggles => new string[] { RebarFeatureToggles.NotifierType };
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
            immutablePassthroughNode.Initialize(elementCreateInfo);
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
            mutablePassthroughNode.Initialize(elementCreateInfo);
            return mutablePassthroughNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }

    #endregion
}
