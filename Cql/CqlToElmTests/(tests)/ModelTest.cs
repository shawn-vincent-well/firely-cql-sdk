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
    [TestClass]
    public class ModelTest : Base
    {
        [TestMethod]
        public void Quantity_Resolves_To_The_Only_Declared_Model()
        {
            // The specification's own example (Developer's Guide, Multiple Data Models): "The
            // identifier Quantity in this function declaration resolves to FHIR.Quantity
            // unambiguously because only the FHIR model is explicitly declared." The implicit
            // System declaration is not considered when determining ambiguity. This used to be
            // reported as ambiguous, which made `(O.value as Quantity).value` -- the ordinary way
            // to read an Observation's numeric value -- an error in every library that did not
            // write FHIR.Quantity.
            var cqlToolkit = CreateCqlToolkit();
            var lib = cqlToolkit.MakeLibrary("""
                                         library Test version '1.0.0'

                                         using FHIR version '4.0.1'

                                         define function f(q Quantity): q
                                         """);
            var or = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<OperandRef>();
            or.resultTypeName.Name.Should().Be("{http://hl7.org/fhir}Quantity");
        }

        [TestMethod]
        public void Quantity_Is_Ambiguous_When_System_Is_Declared()
        {
            // With System declared explicitly the two models are on equal footing and the name is
            // ambiguous, exactly as before; AmbiguousTypeBehavior still decides this case.
            var cqlToolkit = CreateCqlToolkit();
            cqlToolkit.MakeLibrary("""
                                         library Test version '1.0.0'

                                         using System
                                         using FHIR version '4.0.1'

                                         define function f(q Quantity): q
                                         """,
                                         "The type Quantity is ambiguous between System.Quantity, FHIR.Quantity.");
        }

        [TestMethod]
        public void Cast_To_Quantity_Reads_The_FHIR_Type()
        {
            var lib = CreateCqlToolkit().MakeLibrary("""
                library Test version '1.0.0'
                using FHIR version '4.0.1'
                define "Values": [Observation] O return (O.value as Quantity).value
                """);
            var query = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Query>();
            var listType = query.resultTypeSpecifier.Should().BeOfType<ListTypeSpecifier>().Subject;
            listType.elementType.Should().BeOfType<NamedTypeSpecifier>()
                .Which.name.Name.Should().Be("{http://hl7.org/fhir}decimal");
        }


        [TestMethod]
        public void Quantity_Prefer_System()
        {
            var cqlToolkit = CreateCqlToolkit(AmbiguousTypeBehavior: AmbiguousTypeBehavior.PreferSystem);
            var lib = cqlToolkit.MakeLibrary("""
                library Test version '1.0.0'

                using FHIR version '4.0.1'

                define function f(q Quantity): q
                """);
            var or = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<OperandRef>();
            or.Should().HaveType(SystemTypes.QuantityType);
        }

        [TestMethod]
        public void Quantity_Prefer_Model()
        {
            var cqlToolkit = CreateCqlToolkit(AmbiguousTypeBehavior: AmbiguousTypeBehavior.PreferModel);
            var lib = cqlToolkit.MakeLibrary("""
                library Test version '1.0.0'

                using FHIR version '4.0.1'

                define function f(q Quantity): q
                """);
            var or = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<OperandRef>();
            or.resultTypeName.Name.Should().Be("{http://hl7.org/fhir}Quantity");
        }
    }
}
