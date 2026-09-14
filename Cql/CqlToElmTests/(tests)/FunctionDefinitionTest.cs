/*
 * Copyright (c) 2026, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.Abstractions;
using Hl7.Cql.Compiler;
using Hl7.Cql.Compiler.CodeModel;
using Hl7.Cql.Elm;
using Hl7.Cql.Runtime;

namespace Hl7.Cql.CqlToElm.Test
{
    [TestClass]
    public class FunctionDefinitionTest : Base
    {
        private FunctionDef shouldDefineFunction(Library l, string name) =>
            l.ShouldDefine<FunctionDef>(name);

        [TestMethod]
        public void DefinePrivateFluentFunction()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                                                     library FuncTest version '1.0.0'
                                                     using FHIR version '4.0.1'
                                                     context Patient
                                                     define private fluent function Two(): 2
                                                     """);
            var f = shouldDefineFunction(library, "Two");
            f.resultTypeSpecifier.Should().Be(SystemTypes.IntegerType);
            f.expression.Should().BeLiteralInteger(2);
            f.fluent.Should().BeTrue();
            f.accessLevel.Should().Be(AccessModifier.Private);
            f.context.Should().Be("Patient");
        }

        [TestMethod]
        public void DefinePublicNonFluentExternalFunction()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                                                     library FuncTest version '1.0.0'
                                                     define public function Ext() returns String: external
                                                     """);
            var f = shouldDefineFunction(library, "Ext");
            f.resultTypeSpecifier.Should().Be(SystemTypes.StringType);
            f.expression.Should().BeNull();
            f.fluent.Should().BeFalse();
            f.accessLevel.Should().Be(AccessModifier.Public);
        }

        [TestMethod]
        public void UntypedFunctionsAreIllegal()
        {
            _ = CreateCqlToolkit().MakeLibrary("""
                                           library FuncTest version '1.0.0'
                                           define public function Ext(): external
                                           """,
                                           "External functions must specify a return type.");
        }


        [TestMethod]
        public void OptionalReturnTypesMayBeTheSame()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                library FuncTest version '1.0.0'
                define function Two() returns Integer: 2
                """);

            var f = shouldDefineFunction(library, "Two");
            f.resultTypeSpecifier.Should().Be(SystemTypes.IntegerType);
        }

        [TestMethod]
        public void OptionalReturnTypesMayBeSupertype()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                library FuncTest version '1.0.0'
                define function Two() returns Any: 2
                """);

            var f = shouldDefineFunction(library, "Two");
            f.resultTypeSpecifier.Should().Be(SystemTypes.AnyType);
        }

        [TestMethod]
        public void MakesParametersVisibleInScope()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                                                     library FuncTest version '1.0.0'
                                                     define function Double(a Integer): a*2
                                                     """);

            var f = shouldDefineFunction(library, "Double");
            f.resultTypeSpecifier.Should().Be(SystemTypes.IntegerType);
            var mul = f.expression.Should().BeOfType<Multiply>().Subject;
            mul.operand[0].Should().BeOfType<OperandRef>().Which.name.Should().Be("a");
        }

        [TestMethod]
        public void SignalsUnknownParameters()
        {
            CreateCqlToolkit().MakeLibrary("""
                                       library FuncTest version '1.0.0'
                                       define function Double(a Integer): b
                                       """,
                                       "Could not resolve identifier b in the current library.");
        }

        [TestMethod]
        public void ResolvesInParentScope()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                                                     library FuncTest version '1.0.0'
                                                     define b: 5
                                                     define function Double(a Integer): b
                                                     """);

            var f = shouldDefineFunction(library, "Double");
            f.resultTypeSpecifier.Should().Be(SystemTypes.IntegerType);
            var expr = f.expression.Should().BeOfType<ExpressionRef>().Subject;
            expr.name.Should().Be("b");
            expr.libraryName.Should().BeNull();
        }

        [TestMethod]
        public void ResolvesNearestScope()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                                                     library FuncTest version '1.0.0'
                                                     define a: 5
                                                     define function Replace(a String): a
                                                     """);

            var f = shouldDefineFunction(library, "Replace");
            f.resultTypeSpecifier.Should().Be(SystemTypes.StringType);
            f.expression.Should().BeOfType<OperandRef>().Which.name.Should().Be("a");
        }

        [TestMethod]
        public void DoesForwardsReference()
        {
            var library = CreateCqlToolkit().MakeLibrary("""
                                                     library FuncTest version '1.0.0'
                                                     define b: a
                                                     define a: 5
                                                     """);

            var f = library.ShouldDefine<ExpressionDef>("b");
            f.resultTypeSpecifier.Should().Be(SystemTypes.IntegerType);
            f.expression.Should().BeOfType<ExpressionRef>().Which.name.Should().Be("a");
        }

        [TestMethod]
        public void DetectsCycle()
        {
            _ = CreateCqlToolkit().MakeLibrary("""
                                           library FuncTest version '1.0.0'
                                           define a: b
                                           define b: a
                                           """,
                                           "Cannot resolve reference to expression or function a because it results in a circular reference.");
        }

        [TestMethod]
        public void CallsFluentOnMember()
        {
            var lib = CreateCqlToolkit().MakeLibrary("""
                                                 library FuncTest version '1.0.0'

                                                 using FHIR version '4.0.1'

                                                 define fluent function "To date interval"(period FHIR.Period):
                                                     Interval[date from start of period, date from end of period]

                                                 define fluent function "Interval"(coverage FHIR.Coverage):
                                                     (coverage.period as FHIR.Period)."To date interval"()

                                                 """);
            lib.GetErrors().Should().BeEmpty();
        }

        [TestMethod]
        public void CallsFluentOnMember_AcrossLibrary()
        {
            var cqlToolkit = CreateCqlToolkit();
            var fluentLib = cqlToolkit.MakeLibrary("""
                                                         library FluentLib version '1.0.0'

                                                         using FHIR version '4.0.1'

                                                         define fluent function "To date interval"(period FHIR.Period):
                                                             Interval[date from start of period, date from end of period]
                                                         """);

            var testLib = cqlToolkit.MakeLibrary("""
                                                library FuncTest version '1.0.0'

                                                using FHIR version '4.0.1'

                                                include FluentLib version '1.0.0'

                                                define fluent function "Interval"(coverage FHIR.Coverage):
                                                    (coverage.period as FHIR.Period)."To date interval"()
                                                """);
            testLib.GetErrors().Should().BeEmpty();
            testLib.statements.Should().NotBeNull();
            testLib.statements.Should().HaveCount(1);
            var subject = testLib.statements[0].Should().BeOfType<FunctionDef>().Subject;
        }

        [TestMethod]
        public void BirthdatePlusAge()
        {
            var cqlToolkit = CreateCqlToolkit().AddFHIRHelpers();;
            var lib = cqlToolkit.MakeLibrary("""
                                         library Test version '1.0.0'

                                         using FHIR version '4.0.1'

                                         include FHIRHelpers version '4.0.1'

                                         define function f(patient FHIR.Patient, condition FHIR.Condition):
                                           patient.birthDate + (condition.onset as FHIR.Age)
                                         """);
            var add = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Add>();
            add.operand.Should().HaveCount(2);
            var toDate = add.operand[0].Should().BeOfType<FunctionRef>().Subject;
            Assert.AreEqual("FHIRHelpers", toDate.libraryName);
            Assert.AreEqual("ToDate", toDate.name);
            Assert.AreEqual(1, toDate.operand?.Length);
            Assert.AreEqual(SystemTypes.DateType, toDate.resultTypeSpecifier);
            var toQuantity = add.operand[1].Should().BeOfType<FunctionRef>().Subject;
            Assert.AreEqual("FHIRHelpers", toQuantity.libraryName);
            Assert.AreEqual("ToQuantity", toQuantity.name);
            Assert.AreEqual(1, toQuantity.operand?.Length);
            Assert.AreEqual(SystemTypes.QuantityType, toQuantity.resultTypeSpecifier);
        }

        [TestMethod]
        public void ComplexCaseStatement()
        {
            var lib = CreateCqlToolkit().MakeLibrary("""
                                                 library FuncTest version '1.0.0'

                                                 using FHIR version '4.0.1'

                                                 define fluent function "Onset date"(condition FHIR.Condition, birthDate Date):
                                                 case
                                                     when condition is null or condition.onset is null then
                                                         null
                                                     when condition.onset is FHIR.dateTime then
                                                         date from (condition.onset as FHIR.dateTime)
                                                     when condition.onset is FHIR.Period then
                                                         date from start of (condition.onset as FHIR.Period)
                                                     when condition.onset is FHIR.Age and birthDate is not null then
                                                         birthDate + (condition.onset as FHIR.Age)
                                                     when condition.onset is Range and birthDate is not null then
                                                         birthDate + (condition.onset as FHIR.Range).low
                                                     else
                                                         null
                                                 end
                                                 """);
            var cs = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Case>();
            cs.Should().HaveType(SystemTypes.DateType);
        }

        [TestMethod]
        public void External_Function()
        {
            var lib = CreateCqlToolkit().MakeLibrary("""
                library FuncTest version '1.0.0'

                define function Add(left Integer, right Integer) returns Integer: external
                """);
            lib.statements.Should().HaveCount(1);
            var fd = lib.statements[0].Should().BeOfType<FunctionDef>().Subject;
            fd.external.Should().BeTrue();
            fd.externalSpecified.Should().BeTrue();
            fd.expression.Should().BeNull();
        }

        [TestMethod]
        public void CSharp_Keyword_Parameter_Name()
        {
            var cqlToolkit = CreateCqlToolkit();
            var cqlLibraryString = CqlLibraryString.Parse(
                """
                library FuncTest version '1.0.0'

                define function ToInteger(decimal System.Decimal) returns System.Integer: external
                """);

            var lib = cqlToolkit.MakeLibrary(cqlLibraryString.Cql);
            var irDefinitionDictionary = cqlToolkit.CreateElmToolkit().ProcessLibrary(lib);
            // Unlike the old CqlLambdaDefinition, the IR lambda does not carry a leading
            // CqlContext parameter (see CqlLambdaDefinition remarks) -- so its Parameters list is
            // CQL-operand-only and the index shifts down by one. The registered
            // DefinitionSignature, however, is a documented parity quirk for EXTERNAL functions
            // specifically (see CodeBuilderContext.LibraryDefs.cs's HandleExternalFunction
            // NOTE): it still carries a synthetic leading typeof(CqlContext) entry, faithfully
            // replicating the old builder's signature for this lookup.
            var cqlDefinition = irDefinitionDictionary["FuncTest-1.0.0", new DefinitionSignature("ToInteger", typeof(CqlContext), typeof(decimal?))] is CqlLambdaDefinition ld ? ld.Lambda : null;
            cqlDefinition.Should().NotBeNull();
            cqlDefinition!.Parameters.Should().HaveCount(1);
            cqlDefinition.Parameters[0].NameHint.Should().Be("decimal");

            var act = () =>
            {
                using var librarySetInvoker = cqlToolkit.CreateLibrarySetInvoker();
                _ = librarySetInvoker;
            };
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ExpressionAndFunctionMayShareAName()
        {
            // A function is only ever referenced with an argument list, so nothing in the syntax is
            // ambiguous when an expression carries the same name, and the HL7 reference translator
            // accepts such a library. This one was rejected as an identifier already in use and then,
            // because the bare name resolved to the function, died with NotSupportedException from
            // OverloadedFunctionDef.ToRef before that error could be reported.
            var library = CreateCqlToolkit().MakeLibrary("""
                library SameName version '1.0.0'
                define function "Span"(a Integer, b Integer): a + b
                define "Span": 1
                define "By name": "Span"
                define "By call": Span(1, 2)
                """);
            library.statements.OfType<FunctionDef>().Should().ContainSingle(s => s.name == "Span")
                .Which.resultTypeSpecifier.Should().Be(SystemTypes.IntegerType);
            library.statements.Where(s => s is not FunctionDef).Should().ContainSingle(s => s.name == "Span")
                .Which.expression.Should().BeLiteralInteger(1);
            library.ShouldDefine<ExpressionDef>("By name").expression.Should().BeOfType<ExpressionRef>()
                .Which.name.Should().Be("Span");
            library.ShouldDefine<ExpressionDef>("By call").expression.Should().BeOfType<FunctionRef>()
                .Which.name.Should().Be("Span");
        }

        [TestMethod]
        public void BareReferenceToAFunctionIsATranslationError()
        {
            // Not a NotSupportedException. The name resolves to a function and nothing else, and a
            // function cannot be referenced without an argument list.
            CreateCqlToolkit().MakeLibrary("""
                library BareFunction version '1.0.0'
                define function "Span"(a Integer, b Integer): a + b
                define "x": "Span"
                """, "Span is a function and must be invoked with an argument list; no expression named Span is defined.");
        }

        [TestMethod]
        public void TwoExpressionsWithOneNameAreStillRejected()
        {
            CreateCqlToolkit().MakeLibrary("""
                library Twice version '1.0.0'
                define "Span": 1
                define "Span": 2
                """, "Identifier Span is already in use in this library.");
        }
    }
}
