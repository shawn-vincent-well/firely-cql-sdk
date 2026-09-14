/*
 * Copyright (c) 2026, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

#nullable enable

using System.Text.Json.Nodes;
using Hl7.Cql.Elm;

namespace Hl7.Cql.CqlToElm.Test
{
    using BinaryExpression = Hl7.Cql.Elm.BinaryExpression;

    /// <summary>
    /// Round-trips translated ELM through the JSON serializer.
    /// <para>
    /// The rest of this test project asserts on the in-memory ELM tree and on the evaluated result, so
    /// a value that is present on the object but lost on the way to JSON is invisible to it. These tests
    /// close that gap for the ELM node kinds whose value is optional in the schema and whose "no value"
    /// and "value happens to be the CLR default" are therefore different statements.
    /// </para>
    /// </summary>
    [TestClass]
    public class ElmJsonSerializationTest : Base
    {
        /// <summary>
        /// A <c>Quantity</c> of zero must serialize its <c>value</c>. ELM models <c>Quantity.value</c> as
        /// optional, so a consumer reading a <c>Quantity</c> without one sees an absent value, not a zero:
        /// it either evaluates the expression against null or rejects the node outright. Either way it is
        /// answering a different question than the CQL asked.
        /// </summary>
        [TestMethod]
        public void Quantity_WithZeroValue_KeepsValueWhenSerializedToJson()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("0'cm' > 1'cm'");

            var json = library.SerializeToJson();

            var quantities = FindElmNodes(json, "Quantity");
            quantities.Should().HaveCount(2);
            quantities.Select(q => (decimal?)q["value"]).Should().Equal(0m, 1m);
            quantities.Select(q => (string?)q["unit"]).Should().Equal("cm", "cm");

            // The string above pins the shape; reading it back with the serializer's own parser pins that
            // the zero survives as a value rather than as an absence.
            AssertQuantityOperands(json, (0m, "cm"), (1m, "cm"));
        }

        /// <summary>
        /// The same property in a different position: <c>Ratio</c> holds its quantities as properties
        /// rather than as operands, so it exercises the non-polymorphic serialization path.
        /// </summary>
        [TestMethod]
        public void Ratio_WithZeroNumerator_KeepsValueWhenSerializedToJson()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("0'cm' : 1'cm'");

            var json = library.SerializeToJson();

            var ratio = FindElmNodes(json, "Ratio").Should().ContainSingle().Subject;
            ((decimal?)ratio["numerator"]?["value"]).Should().Be(0m);
            ((decimal?)ratio["denominator"]?["value"]).Should().Be(1m);

            var roundTripped = Library.ParseFromJson(json, validate: false);
            var parsedRatio = roundTripped.statements[0].expression.Should().BeOfType<Ratio>().Subject;
            (parsedRatio.numerator.value, parsedRatio.numerator.valueSpecified).Should().Be((0m, true));
            (parsedRatio.denominator.value, parsedRatio.denominator.valueSpecified).Should().Be((1m, true));
        }

        /// <summary>
        /// A zero <c>Literal</c> carries its value as a string, so it is not exposed to the same
        /// default-value omission. Pinned here so that a change to the omission rules cannot silently
        /// start dropping it.
        /// </summary>
        [TestMethod]
        public void Literal_WithZeroValue_KeepsValueWhenSerializedToJson()
        {
            var library = CreateCqlToolkit().MakeLibraryFromExpression("0 > 1");

            var json = library.SerializeToJson();

            var literals = FindElmNodes(json, "Literal");
            literals.Should().HaveCount(2);
            literals.Select(l => (string?)l["value"]).Should().Equal("0", "1");
        }

        private static void AssertQuantityOperands(string json, params (decimal value, string unit)[] expected)
        {
            var roundTripped = Library.ParseFromJson(json, validate: false);
            var operand = roundTripped.statements[0].expression.Should().BeAssignableTo<BinaryExpression>().Subject.operand;
            operand.Should().HaveCount(expected.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                var quantity = operand[i].Should().BeOfType<Quantity>().Subject;
                quantity.value.Should().Be(expected[i].value);
                quantity.unit.Should().Be(expected[i].unit);
                quantity.valueSpecified.Should().BeTrue();
            }
        }

        /// <summary>
        /// Collects every JSON object carrying the given ELM type discriminator, in document order.
        /// </summary>
        private static List<JsonObject> FindElmNodes(string json, string elmType)
        {
            var found = new List<JsonObject>();
            collect(JsonNode.Parse(json));
            return found;

            void collect(JsonNode? node)
            {
                switch (node)
                {
                    case JsonObject o:
                        if ((string?)o["type"] == elmType)
                            found.Add(o);
                        foreach (var (_, value) in o)
                            collect(value);
                        break;
                    case JsonArray a:
                        foreach (var item in a)
                            collect(item);
                        break;
                }
            }
        }
    }
}
