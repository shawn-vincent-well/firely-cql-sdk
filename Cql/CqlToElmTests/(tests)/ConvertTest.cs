/*
 * Copyright (c) 2025, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.Elm;
using Hl7.Cql.Primitives;

namespace Hl7.Cql.CqlToElm.Test
{
    [TestClass]
    public class ConvertTest : Base
    {
        [TestMethod]
        public void Convert_Integer_to_String()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 5 to String");
            library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToString>();
        }

        [TestMethod]
        public void Convert_String_to_DateTime()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("ToDateTime('2014-01-01T12:05:05.955-01:15')");
            var toDateTime = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToDateTime>();
            var result = Run(toDateTime, library);
            var dt = result.Should().BeOfType<CqlDateTime>().Subject;
            dt.Value.OffsetHour.Should().Be(-1);
            dt.Value.OffsetMinute.Should().Be(-15);
            dt.Value.DateTimeOffset.Offset.Should().Be(TimeSpan.FromMinutes(-75));
        }

        [TestMethod]
        public void Convert_Code_To_Concept()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("ToConcept(Code { code: '8480-6' })");
            var toConcept = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToConcept>();
            var result = Run<CqlConcept>(toConcept, library);
        }

        [TestMethod]
        public void Convert_Quantity_To_Weeks()
        {
            var lib = CreateCqlToolkit().MakeLibrary("""

                                library Test version '1.0.0'

                                define function f(q Quantity):
                                    convert q to weeks
                """);
            lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ConvertQuantity>();

        }

        [TestMethod]
        public void Convert_Meters_to_Centimeters()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 5 'm' to 'cm'");
            var quantity = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ConvertQuantity>();
            var result = Run(quantity, library);
            var q = result.Should().BeOfType<CqlQuantity>().Subject;
            q.value.Should().Be(500m);
            q.unit.Should().Be("cm");
        }

        [TestMethod]
        [Ignore("Mass units are not yet defined and respective conversions not yet implemented, see UcumUnits.cs and UnitConverter.cs")]
        public void Convert_Grams_to_Kilograms()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 500 'g' to 'kg'");
            var quantity = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ConvertQuantity>();
            var result = Run(quantity, library);
            var q = result.Should().BeOfType<CqlQuantity>().Subject;
            q.value.Should().Be(0.5m);
            q.unit.Should().Be("kg");
        }

        [TestMethod]
        public void Convert_InvalidString_to_Integer_ReturnsNull()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 'foo' to Integer");
            var convert = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Elm.Convert>();
            var result = Run(convert, library);
            result.Should().BeNull();
        }

        [TestMethod]
        public void Convert_ValidString_to_Integer_ReturnsValue()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert '123' to Integer");
            var convert = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Elm.Convert>();
            var result = Run(convert, library);
            result.Should().Be(123);
        }

        [TestMethod]
        public void Convert_TrueString_to_Boolean_ReturnsTrue()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 'true' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().Be(true);
        }

        [TestMethod]
        public void Convert_TrueStringWithCapitalization_to_Boolean_ReturnsTrue()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 'True' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().Be(true);
        }

        [TestMethod]
        public void Convert_FalseString_to_Boolean_ReturnsFalse()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 'false' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().Be(false);
        }

        [TestMethod]
        public void Convert_YesString_to_Boolean_ReturnsTrue()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 'yes' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().Be(true);
        }

        [TestMethod]
        public void Convert_OneString_to_Boolean_ReturnsTrue()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert '1' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().Be(true);
        }

        [TestMethod]
        public void Convert_NoString_to_Boolean_ReturnsFalse()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 'no' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().Be(false);
        }

        [TestMethod]
        public void Convert_ZeroString_to_Boolean_ReturnsFalse()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert '0' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().Be(false);
        }

        [TestMethod]
        public void Convert_InvalidString_to_Boolean_ReturnsNull()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("convert 'foo' to Boolean");
            var toBoolean = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToBoolean>();
            var result = Run(toBoolean, library);
            result.Should().BeNull();
        }

        // The ConvertsToX(argument Any) Boolean family, ConvertQuantity, CanConvertQuantity,
        // GeometricMean and the Boolean/String overloads of ToLong are CQL 1.5 operators the runtime
        // implements. They were missing from the System library declarations, so a library using
        // one failed to translate with "Could not resolve call to operator ...". Each test
        // translates AND runs, so the ELM node reaches the runtime binding as well.

        [DataTestMethod]
        [DataRow("ConvertsToBoolean('true')", typeof(ConvertsToBoolean), true)]
        [DataRow("ConvertsToBoolean('maybe')", typeof(ConvertsToBoolean), false)]
        [DataRow("ConvertsToDate('2014-01-01')", typeof(ConvertsToDate), true)]
        [DataRow("ConvertsToDateTime('2014-01-01T12:00:00')", typeof(ConvertsToDateTime), true)]
        [DataRow("ConvertsToDecimal('1.5')", typeof(ConvertsToDecimal), true)]
        [DataRow("ConvertsToInteger('42')", typeof(ConvertsToInteger), true)]
        [DataRow("ConvertsToInteger('4.2')", typeof(ConvertsToInteger), false)]
        [DataRow("ConvertsToInteger(4.2)", typeof(ConvertsToInteger), false)]
        [DataRow("ConvertsToInteger(4294967296L)", typeof(ConvertsToInteger), false)]
        [DataRow("ConvertsToBoolean(1)", typeof(ConvertsToBoolean), true)]
        [DataRow("ConvertsToBoolean(5)", typeof(ConvertsToBoolean), false)]
        [DataRow("ConvertsToLong('42')", typeof(ConvertsToLong), true)]
        [DataRow("ConvertsToQuantity('5 \\'mg\\'')", typeof(ConvertsToQuantity), true)]
        [DataRow("ConvertsToString(42)", typeof(ConvertsToString), true)]
        [DataRow("ConvertsToTime('12:30:00')", typeof(ConvertsToTime), true)]
        [DataRow("ConvertsToTime('noon')", typeof(ConvertsToTime), false)]
        public void ConvertsTo_Predicates(string cql, Type node, bool expected)
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression(cql);
            var expression = library.statements.Should().ContainSingle().Which.expression;
            expression.Should().BeOfType(node);
            expression.resultTypeSpecifier.Should().Be(SystemTypes.BooleanType);
            Run(expression, library).Should().Be(expected);
        }

        [TestMethod]
        public void ConvertsTo_Null_Is_Null()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("ConvertsToInteger(null)");
            var expression = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ConvertsToInteger>();
            Run(expression, library).Should().BeNull();
        }

        [TestMethod]
        public void ConvertQuantity_By_Unit_Name()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("ConvertQuantity(5 'mg', 'g')");
            var expression = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ConvertQuantity>();
            expression.resultTypeSpecifier.Should().Be(SystemTypes.QuantityType);
            var q = Run(expression, library).Should().BeOfType<CqlQuantity>().Subject;
            q.unit.Should().Be("g");
            q.value.Should().Be(0.005m);
        }

        [DataTestMethod]
        [DataRow("CanConvertQuantity(5 'mg', 'g')", true)]
        [DataRow("CanConvertQuantity(5 'mg', 'cm')", false)]
        public void CanConvertQuantity_By_Unit_Name(string cql, bool expected)
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression(cql);
            var expression = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<CanConvertQuantity>();
            expression.resultTypeSpecifier.Should().Be(SystemTypes.BooleanType);
            Run(expression, library).Should().Be(expected);
        }

        [TestMethod]
        public void GeometricMean_Of_Decimals()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("GeometricMean({ 1.0, 4.0, 16.0 })");
            var expression = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<GeometricMean>();
            expression.resultTypeSpecifier.Should().Be(SystemTypes.DecimalType);
            Run(expression, library).Should().Be(4.0m);
        }

        [DataTestMethod]
        [DataRow("ToLong('42')", 42L)]
        [DataRow("ToLong(true)", 1L)]
        [DataRow("ToLong(42)", 42L)]
        public void ToLong_Overloads(string cql, long expected)
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression(cql);
            var expression = library.Should().BeACorrectlyInitializedLibraryWithStatementOfType<ToLong>();
            expression.resultTypeSpecifier.Should().Be(SystemTypes.LongType);
            Run(expression, library).Should().Be(expected);
        }
    }
}
