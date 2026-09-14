/*
 * Copyright (c) 2025, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.Elm;

namespace Hl7.Cql.CqlToElm.Test
{
    using Tuple = Hl7.Cql.Elm.Tuple;
    using Expression = Hl7.Cql.Elm.Expression;

    [TestClass]
    public class CoercionTest : Base
    {
        static CoercionTest()
        {
            var fluentCqlToolkit = CreateCqlToolkit(
                DisableListPromotion: false,
                DisableListDemotion: false);
            ElmFactory = fluentCqlToolkit.GetElmFactory();
            CoercionProvider = fluentCqlToolkit.GetCoercionProvider();
        }

        private static readonly CoercionProvider CoercionProvider;

        private static readonly ElmFactory ElmFactory;

        private static Null Null() => new Null().WithResultType(SystemTypes.AnyType);
        private static Null Null(TypeSpecifier type) => new Null().WithResultType(type);

        private static Literal String(string value = "") => ElmFactory.Literal(value);
        private static Literal Integer(int value = 1) => ElmFactory.Literal(value);

        private static Interval Interval(Expression low, Expression high) =>
            new Interval { low = low, high = high }.WithResultType(low.resultTypeSpecifier.ToIntervalType());

        private static ValueSetRef ValueSet(string url = "http://hl7.org") =>
            new ValueSetRef { name = "TestValueSet" }.WithResultType(SystemTypes.ValueSetType);

        private static List List(TypeSpecifier elementType, params Expression[] elements) =>
            new List { element = elements }.WithResultType(elementType.ToListType());

        private static ChoiceTypeSpecifier Choice(params TypeSpecifier[] types) => new ChoiceTypeSpecifier(types);

        private static Tuple Tuple(params (string name, Expression value)[] tuple) =>
            new Tuple { element = tuple.Select(t => new TupleElement { name = t.name, value = t.value }).ToArray() }
                .WithResultType(TupleType(tuple.Select(t => (t.name, t.value.resultTypeSpecifier)).ToArray()));

        private static TypeSpecifier TupleType(params (string name, TypeSpecifier type)[] tuple) =>
            new TupleTypeSpecifier { element = tuple.Select(t => new TupleElementDefinition { name = t.name, elementType = t.type }).ToArray() };

        private static ParameterTypeSpecifier T = new ParameterTypeSpecifier { parameterName = "T" };

        [TestMethod]
        public void IntegerExactyMatchesInteger()
        {
            var expression = Integer();
            var result = CoercionProvider.Coerce(expression, SystemTypes.IntegerType);
            Assert.IsTrue(result.Success);
            Assert.AreSame(expression, result.Result);
            Assert.AreEqual(CoercionCost.ExactMatch, result.Cost);
        }

        [TestMethod]
        public void ChoiceIsExactlyMatchesChoice()
        {
            var expression = new Null().WithResultType(Choice(SystemTypes.IntegerType, SystemTypes.StringType));
            var result = CoercionProvider.Coerce(expression, Choice(SystemTypes.IntegerType, SystemTypes.StringType));
            Assert.IsTrue(result.Success);
            Assert.AreSame(expression, result.Result);
            Assert.AreEqual(CoercionCost.ExactMatch, result.Cost);
        }

        [TestMethod]
        public void TupleExactlyMatchesTuple()
        {
            var expression = Tuple(("x", Integer(1)), ("y", Integer(2)));
            var tupleType = TupleType(("y", SystemTypes.IntegerType), ("x", SystemTypes.IntegerType));
            var result = CoercionProvider.Coerce(expression, tupleType);
            Assert.IsTrue(result.Success);
            Assert.AreSame(expression, result.Result);
            Assert.AreEqual(CoercionCost.ExactMatch, result.Cost);
        }

        [TestMethod]
        public void TupleDoesNotExactlMatchesTuple()
        {
            var expression = Tuple(("x", Integer(1)), ("y", Integer(2)));
            var tupleType = TupleType(("y", SystemTypes.IntegerType), ("x", SystemTypes.LongType));
            Assert.IsFalse(CoercionProvider.IsExactMatch(expression.resultTypeSpecifier, tupleType));
        }

        [TestMethod]
        public void TupleHasImplicitConversionToIdenticalTuple()
        {
            var from = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.IntegerType));
            var to = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.IntegerType));
            Assert.IsTrue(CoercionProvider.HasImplicitConversion(from, to));
        }

        [TestMethod]
        public void TupleHasImplicitConversionWhenFieldsReordered()
        {
            var from = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.StringType));
            var to = TupleType(("y", SystemTypes.StringType), ("x", SystemTypes.IntegerType));
            Assert.IsTrue(CoercionProvider.HasImplicitConversion(from, to));
        }

        [TestMethod]
        public void TupleHasNoImplicitConversionWhenNonFirstFieldIncompatible()
        {
            var from = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.StringType));
            var to = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.CodeType));
            Assert.IsFalse(CoercionProvider.HasImplicitConversion(from, to));
        }

        [TestMethod]
        public void TupleHasNoImplicitConversionWhenFieldNameMissing()
        {
            var from = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.IntegerType));
            var to = TupleType(("x", SystemTypes.IntegerType), ("z", SystemTypes.IntegerType));
            Assert.IsFalse(CoercionProvider.HasImplicitConversion(from, to));
        }

        [TestMethod]
        public void TupleHasImplicitConversionWhenFieldConvertible()
        {
            var from = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.IntegerType));
            var to = TupleType(("x", SystemTypes.IntegerType), ("y", SystemTypes.DecimalType));
            Assert.IsTrue(CoercionProvider.HasImplicitConversion(from, to));
        }

        [TestMethod]
        public void IntegerIsSubtypeOfAny()
        {
            var expression = Integer();
            // <ns4:typeInfo xsi:type="ns4:SimpleTypeInfo" name="System.Integer" baseType="System.Any"/>
            var result = CoercionProvider.Coerce(expression, SystemTypes.AnyType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.Subtype, result.Cost);
        }

        [TestMethod]
        public void AnyIsSubtypeOfAny()
        {
            Assert.IsTrue(CoercionProvider.IsSubtype(SystemTypes.AnyType, SystemTypes.AnyType));
        }

        [TestMethod]
        public void IntegerIsNotSubtypeOfLong()
        {
            Assert.IsFalse(CoercionProvider.IsSubtype(SystemTypes.IntegerType, SystemTypes.LongType));
        }

        [TestMethod]
        public void ValueSetIsSubtypeOfVocabulary()
        {
            var expression = ValueSet();
            // <ns4:typeInfo xsi:type="ns4:ClassInfo" name="System.ValueSet" baseType="System.Vocabulary"/>
            var result = CoercionProvider.Coerce(expression, SystemTypes.VocabularyType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.Subtype, result.Cost);
        }

        [TestMethod]
        public void ValueSetIsSubtypeOfAny()
        {
            var expression = ValueSet();
            // <ns4:typeInfo xsi:type="ns4:ClassInfo" name="System.ValueSet" baseType="System.Vocabulary"/>
            // <ns4:typeInfo xsi:type="ns4:ClassInfo" name="System.Vocabulary" baseType="System.Any">
            var result = CoercionProvider.Coerce(expression, SystemTypes.AnyType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.Subtype, result.Cost);
        }

        [TestMethod]
        public void IntegerCanBeCastToChoice()
        {
            var expression = Integer();
            var result = CoercionProvider.Coerce(expression, Choice(SystemTypes.IntegerType, SystemTypes.StringType));
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.Cast, result.Cost);
        }

        [TestMethod]
        public void ChoiceIsCompatibleWithChoice()
        {
            var expression = new Null().WithResultType(Choice(SystemTypes.IntegerType, SystemTypes.StringType));
            var result = CoercionProvider.Coerce(expression, Choice(SystemTypes.IntegerType, SystemTypes.DecimalType));
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.Cast, result.Cost);
        }

        [TestMethod]
        public void AnyIsCompatibleWithInteger()
        {
            var expression = Null();
            var result = CoercionProvider.Coerce(expression, SystemTypes.IntegerType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.MoreCompatible, result.Cost);
        }

        [TestMethod]
        public void IntegerHasImplicitConversionToLong()
        {
            var expression = Integer();
            var result = CoercionProvider.Coerce(expression, SystemTypes.LongType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ImplicitToSimpleType, result.Cost);
        }

        [TestMethod]
        public void IntegerCanBePromotedToInterval()
        {
            var expression = Integer();
            var result = CoercionProvider.Coerce(expression, SystemTypes.IntegerType.ToIntervalType());
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.IntervalPromotion, result.Cost);
        }

        [TestMethod]
        public void IntegerCanBePromotedToList()
        {
            var expression = Integer();
            var result = CoercionProvider.Coerce(expression, SystemTypes.IntegerType.ToListType());
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ListPromotion, result.Cost);
        }

        [TestMethod]
        public void IntervalCanBeDemoted()
        {
            var expression = Interval(Integer(1), Integer(2));
            var result = CoercionProvider.Coerce(expression, SystemTypes.IntegerType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.IntervalDemotion, result.Cost);
        }

        [TestMethod]
        public void ListCanBeDemoted()
        {
            var expression = List(SystemTypes.IntegerType, Integer(1), Integer(2));
            var result = CoercionProvider.Coerce(expression, SystemTypes.IntegerType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ListDemotion, result.Cost);
        }

        [TestMethod]
        public void IntegerPromotedToDecimal()
        {
            var expression = Integer(2);
            var result = CoercionProvider.Coerce(expression, SystemTypes.DecimalType);
            Assert.IsTrue(result.Success);
            Assert.IsInstanceOfType(result.Result, typeof(ToDecimal));
            Assert.AreEqual(CoercionCost.ImplicitToSimpleType, result.Cost);
        }

        [TestMethod]
        public void ListAnyCanBeCastToListInteger()
        {
            var expression = List(SystemTypes.AnyType);
            var result = CoercionProvider.Coerce(expression, SystemTypes.IntegerType.ToListType());
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.MoreCompatible, result.Cost);
        }

        [TestMethod]
        public void ListIsSubtypeOfAny()
        {
            var expression = List(SystemTypes.IntegerType);
            var result = CoercionProvider.Coerce(expression, SystemTypes.AnyType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.Subtype, result.Cost);
        }

        [TestMethod]
        public void IntegerCanBeCastAsInteger() =>
            CoercionProvider.CanBeCast(SystemTypes.IntegerType, SystemTypes.IntegerType)
                            .Should().BeTrue();

        [TestMethod]
        public void AnyCanBeCastAsIntervalAny() =>
            CoercionProvider.CanBeCast(SystemTypes.AnyType, SystemTypes.AnyType.ToIntervalType())
                            .Should().BeTrue();

        [TestMethod]
        public void ListIntervalIntegerToListIntervalDecimal()
        {
            var expression = List(SystemTypes.IntegerType.ToIntervalType(),
                                  Interval(Integer(1), Integer(2)));
            var result = CoercionProvider.Coerce(expression,
                                                 SystemTypes.DecimalType.ToIntervalType().ToListType());
            result.Success.Should().BeTrue();
            result.Cost.Should().Be(CoercionCost.ImplicitToSimpleType);
            result.Result.Should().BeOfType<Query>();
        }


        [TestMethod]
        public void IntervalIntegerToIntervalDecimal()
        {
            var expression = Interval(Integer(1), Integer(2));
            var result = CoercionProvider.Coerce(expression,
                                                 SystemTypes.DecimalType.ToIntervalType());
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ImplicitToSimpleType, result.Cost);
        }

        [TestMethod]
        public void IntervalCoercionReadsClosednessFromTheSource()
        {
            // The closedness of an interval that has a value at run time must keep coming from that
            // value, so that it stays consistent with the boundaries, which are read off it too.
            var expression = Interval(Integer(1), Integer(2));

            var result = CoercionProvider.Coerce(expression, SystemTypes.DecimalType.ToIntervalType());

            result.Success.Should().BeTrue();
            var coerced = result.Result.Should().BeOfType<Interval>().Subject;
            coerced.lowClosedExpression.Should().BeOfType<Property>().Which.path.Should().Be("lowClosed");
            coerced.highClosedExpression.Should().BeOfType<Property>().Which.path.Should().Be("highClosed");
        }

        [TestMethod]
        public void NullBoundedIntervalCoercionIsANullInterval()
        {
            // An interval selector with two null boundaries denotes a null interval: with no point
            // type there is no range for either boundary to stand for. Coercing it to Interval<Integer>
            // must therefore yield a null Interval<Integer>. Rebuilding it as an interval with two null
            // boundaries would not: the specification reads a closed null boundary as the beginning or
            // end of the point type's range, so the rebuilt interval would span the whole of Integer.
            var library = CreateCqlToolkit(AllowNullIntervals: true)
                .MakeLibraryFromExpression("Interval(null, null) overlaps Interval[1, 10]");
            var overlaps = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Overlaps>();
            var coerced = overlaps.operand[0].Should().BeOfType<Null>().Subject;
            var type = coerced.resultTypeSpecifier.Should().BeOfType<IntervalTypeSpecifier>().Subject;
            type.pointType.Should().Be(SystemTypes.IntegerType);

            // The assertions above read the in-memory tree. Round-trip the emitted ELM through the
            // reader as well, so the shape is checked as it is actually published, not only as it is
            // built.
            var reread = Library.ParseFromJson(library.SerializeToJson());
            var rereadOverlaps = reread.statements.Should().ContainSingle()
                                       .Which.expression.Should().BeOfType<Overlaps>().Subject;
            rereadOverlaps.operand[0].Should().BeOfType<Null>();
        }

        [TestMethod]
        public void TypedNullBoundedIntervalCoercionStaysAnInterval()
        {
            // `Interval[null as Integer, null as Integer]` is not a null interval: its boundaries are
            // casts, it has a point type, and per the specification it spans the whole of Integer.
            // Coercing it to Interval<Decimal> must keep it an interval.
            var library = CreateCqlToolkit()
                .MakeLibraryFromExpression("Interval[null as Integer, null as Integer] overlaps Interval[1.0, 10.0]");
            var overlaps = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Overlaps>();
            overlaps.operand[0].Should().BeOfType<Interval>();
        }

        [TestMethod]
        public void ValueSetToListCode()
        {
            var expression = ValueSet();
            var result = CoercionProvider.Coerce(expression,
                                                 SystemTypes.CodeType.ToListType());
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ImplicitToClassType, result.Cost);
        }

        [TestMethod]
        public void ListValueSetToListListCodes()
        {
            var expression = List(SystemTypes.ValueSetType, ValueSet(), ValueSet());
            var llc = SystemTypes.CodeType.ToListType().ToListType();
            var result = CoercionProvider.Coerce(expression, llc);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ImplicitToClassType, result.Cost);
            Assert.AreEqual(llc, result.Result.resultTypeSpecifier);
        }

        [TestMethod]
        public void FhirAgeToQuantity()
        {
            // This conversion is defined
            // <conversionInfo functionName="FHIRHelpers.ToQuantity" fromType="FHIR.Quantity" toType="System.Quantity"/>
            // Age is a subtype of FHIR.Quantity

            var qnts = new NamedTypeSpecifier { name = new System.Xml.XmlQualifiedName("{http://hl7.org/fhir}Quantity") };
            var result = CoercionProvider.Coerce(Null(qnts), SystemTypes.QuantityType);
            Assert.IsTrue(result.Success);

            var ageNts = new NamedTypeSpecifier { name = new System.Xml.XmlQualifiedName("{http://hl7.org/fhir}Age") };
            result = CoercionProvider.Coerce(Null(ageNts), SystemTypes.QuantityType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ImplicitToClassType, result.Cost);
            Assert.IsInstanceOfType(result.Result, typeof(FunctionRef));
            var fr = (FunctionRef)result.Result;
        }

        [TestMethod]
        public void FhirDateToSystemDate()
        {
            var fdt = new NamedTypeSpecifier { name = new System.Xml.XmlQualifiedName("{http://hl7.org/fhir}date") };
            var cost = CoercionProvider.GetCoercionCost(fdt, SystemTypes.DateType);
            Assert.AreEqual(CoercionCost.ImplicitToSimpleType, cost);
            var result = CoercionProvider.Coerce(Null(fdt), SystemTypes.DateType);
            Assert.IsInstanceOfType(result.Result, typeof(FunctionRef));
            var fr = (FunctionRef)result.Result;
            Assert.AreEqual("FHIRHelpers", fr.libraryName);
            Assert.AreEqual("ToDate", fr.name);
            Assert.AreEqual(1, fr.operand?.Length);
            Assert.IsInstanceOfType(fr.operand![0], typeof(Null));
            Assert.AreEqual(SystemTypes.DateType, fr.resultTypeSpecifier);
        }

        // Bug 1 fix: System.Date → System.DateTime should use ToDateTime (not As), because
        // DateTime is a SimpleTypeInfo in the system model, so ImplicitCastToSimple is called.
        [TestMethod]
        public void DateToDateTimeProducesToDateTime()
        {
            var cost = CoercionProvider.GetCoercionCost(SystemTypes.DateType, SystemTypes.DateTimeType);
            Assert.AreEqual(CoercionCost.ImplicitToSimpleType, cost);
            var result = CoercionProvider.Coerce(Null(SystemTypes.DateType), SystemTypes.DateTimeType);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ImplicitToSimpleType, result.Cost);
            Assert.IsInstanceOfType(result.Result, typeof(ToDateTime));
            Assert.AreEqual(SystemTypes.DateTimeType, result.Result.resultTypeSpecifier);
        }

        // Bug 2 fix: When the source is a ChoiceTypeSpecifier and exactly one choice type has
        // an implicit model conversion to the target type, the expression should be cast to that
        // choice type and then converted via the model function.
        [TestMethod]
        public void ChoiceWithFhirPeriodToIntervalDateTimeUsesFunctionRef()
        {
            var periodType = new NamedTypeSpecifier { name = new System.Xml.XmlQualifiedName("{http://hl7.org/fhir}Period") };
            var ageType = new NamedTypeSpecifier { name = new System.Xml.XmlQualifiedName("{http://hl7.org/fhir}Age") };
            var rangeType = new NamedTypeSpecifier { name = new System.Xml.XmlQualifiedName("{http://hl7.org/fhir}Range") };
            var choiceType = new ChoiceTypeSpecifier(ageType, periodType, rangeType);
            var expression = Null(choiceType);
            var intervalDateTimeType = SystemTypes.DateTimeType.ToIntervalType();

            var result = CoercionProvider.Coerce(expression, intervalDateTimeType);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(CoercionCost.ImplicitToClassType, result.Cost);
            Assert.IsInstanceOfType(result.Result, typeof(FunctionRef));
            var fr = (FunctionRef)result.Result;
            Assert.AreEqual("FHIRHelpers", fr.libraryName);
            Assert.AreEqual("ToInterval", fr.name);
            Assert.AreEqual(1, fr.operand?.Length);
            Assert.IsInstanceOfType(fr.operand![0], typeof(As));
            var asExpr = (As)fr.operand[0];
            Assert.AreEqual(periodType, asExpr.asTypeSpecifier);
            Assert.AreEqual(intervalDateTimeType, result.Result.resultTypeSpecifier);
        }
    }
}
