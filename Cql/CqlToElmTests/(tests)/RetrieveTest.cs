/*
 * Copyright (c) 2025, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.Elm;
using Hl7.Cql.Fhir;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Hl7.Cql.CqlToElm.Test
{
    [TestClass]
    public class RetrieveTest : Base
    {
        [TestMethod]
        public async Task Retrieve_AllTerms()
        {
            var cqlToolkit = CreateCqlToolkit();
            var cqlLibraryString = CqlLibraryString.Parse("""
                                                          library RetrieveTest version '1.0.0'

                                                          using FHIR version '4.0.1'

                                                          valueset "terminology": 'http://fire.ly/ValueSet/Test'

                                                          context Patient

                                                          define private Retrieve_AllTerms: [Patient->Condition: code in "terminology"]
                                                          """);
            var library = cqlToolkit.MakeLibrary(cqlLibraryString.Cql);
            Assert.IsNotNull(library.statements);
            Assert.AreEqual(2, library.statements.Length);
            Assert.IsNotNull(library.statements[1].expression.localId);
            Assert.IsNotNull(library.statements[1].expression.locator);
            Assert.IsInstanceOfType(library.statements[1].expression, typeof(Retrieve));
            {
                var retrieve = (Retrieve)library.statements[1].expression;
                Assert.IsNotNull(retrieve.context);
                Assert.IsInstanceOfType(retrieve.context, typeof(ExpressionRef));
                {
                    var contextRef = (ExpressionRef)retrieve.context;
                    Assert.AreEqual("Patient", contextRef.name);
                }
                Assert.AreEqual("{http://hl7.org/fhir}Condition", retrieve.dataType?.Name);
                Assert.AreEqual("http://hl7.org/fhir/StructureDefinition/Condition", retrieve.templateId);
                Assert.AreEqual("code", retrieve.codeProperty);
                Assert.AreEqual("in", retrieve.codeComparator);
                Assert.IsNotNull(retrieve.codes);
                Assert.IsInstanceOfType(retrieve.codes, typeof(ValueSetRef));
                {
                    var valueSetRef = (ValueSetRef)retrieve.codes;
                    Assert.AreEqual("terminology", valueSetRef.name);
                }

                var valueSets = await (new[]
                                          {
                                              new ValueSet
                                              {
                                                  Id = "http://fire.ly/ValueSet/Test",
                                                  Url = "http://fire.ly/ValueSet/Test",
                                                  Expansion = new ValueSet.ExpansionComponent
                                                  {
                                                      Contains = new List<ValueSet.ContainsComponent>
                                                      {
                                                          new ValueSet.ContainsComponent
                                                          {
                                                              Code = "code1",
                                                              System = "http://fire.ly/CodeSystem/Test"
                                                          }
                                                      }
                                                  }
                                              }
                                          }.ToValueSetDictionaryAsync());

                var bundle = new Bundle
                {
                    Entry = new List<Bundle.EntryComponent>
                    {
                        new Bundle.EntryComponent
                        {
                            Resource = new Condition
                            {
                                Id = "1"
                            }
                        },
                        new Bundle.EntryComponent
                        {
                            Resource = new Condition
                            {
                                Id = "2",
                                Code = new CodeableConcept
                                {
                                    Coding = new List<Coding>
                                    {
                                        new Coding { Code = "code1", System = "http://fire.ly/CodeSystem/Test" }
                                    }
                                }
                            }
                        }
                    }
                };

                using var librarySetInvoker = cqlToolkit.CreateLibrarySetInvoker();
                var result = librarySetInvoker.InvokeLibraryDefinition(
                    FhirCqlContext.ForBundle(bundle, valueSets: valueSets),
                    cqlLibraryString.LibraryIdentifier, "Retrieve_AllTerms");
                var conditions = result as IEnumerable<Condition>;
                Assert.IsNotNull(conditions);
                var ids = conditions.Select(c => c.Id).ToArray();
                Assert.AreEqual(1, ids.Length);
                Assert.AreEqual("2", ids[0]);
            }
        }


        [TestMethod]
        public void Retrieve_FilteredByCode()
        {
            var cqlToolkit = CreateCqlToolkit();
            var cqlLibraryString = CqlLibraryString.Parse("""
                                                          library FilteredRetrieve version '1.0.0'

                                                          using FHIR version '4.0.1'
                                                          codesystem LOINC: 'http://loinc.org'
                                                          code "Body height": '8302-2' from LOINC

                                                          define "Body height observations":
                                                              [Observation: "Body height"]
                                                          """);
            var lib = cqlToolkit.MakeLibrary(cqlLibraryString.Cql);

            var r = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Retrieve>();
            r.Should().NotBeNull();

            Assert.AreEqual(1, lib.codeSystems.Length);
            Assert.AreEqual(1, lib.codes.Length);
            Assert.AreEqual(1, lib.statements.Length);
            Assert.IsInstanceOfType(lib.statements[0].expression, typeof(Retrieve));

            var retrieve = (Retrieve)lib.statements[0].expression;
            Assert.AreEqual("code", retrieve.codeProperty);
            Assert.AreEqual("~", retrieve.codeComparator);
            Assert.IsNotNull(retrieve.codes);
            Assert.IsInstanceOfType(retrieve.codes, typeof(ToList));

            var toList = (ToList)retrieve.codes;
            Assert.IsInstanceOfType(toList.operand, typeof(CodeRef));

            var codeRef = (CodeRef)toList.operand;
            Assert.AreEqual("Body height", codeRef.name);

            using var librarySetInvoker = cqlToolkit.CreateLibrarySetInvoker();

            var bundle = new Bundle
            {
                Entry = new List<Bundle.EntryComponent>
                {
                    new Bundle.EntryComponent
                    {
                        Resource = new Observation
                        {
                            Id = "1",
                            Code = new CodeableConcept
                            {
                                Coding = new List<Coding>
                                {
                                    new Coding { Code = "8302-2", System = "http://loinc.org" }
                                }
                            }
                        }
                    },
                    new Bundle.EntryComponent
                    {
                        Resource = new Observation
                        {
                            Id = "2",
                            Code = new CodeableConcept
                            {
                                Coding = new List<Coding>
                                {
                                    new Coding { Code = "29463-7", System = "http://loinc.org" }
                                }
                            }
                        }
                    }
                }
            };

            var result = librarySetInvoker.InvokeLibraryDefinition(
                FhirCqlContext.ForBundle(bundle),
                cqlLibraryString.LibraryIdentifier, "Body height observations");

            result.Should().NotBeNull();
            Assert.IsInstanceOfType(result, typeof(IEnumerable<Observation>));
            var observations = (IEnumerable<Observation>)result;
            Assert.AreEqual(1, observations.Count());
        }

        [TestMethod]
        public void Retrieve_FilteredByCodeList()
        {
            var cqlToolkit = CreateCqlToolkit();
            var cqlLibraryString = CqlLibraryString.Parse("""
                                                          library Test version '1.0.0'
                                                          using FHIR version '4.0.1'
                                                          codesystem "LOINC": 'http://loinc.org'
                                                          code "Systolic BP": '8480-6' from "LOINC"
                                                          context Unfiltered
                                                          define q: from [Observation : { "Systolic BP" }] o return o.id
                                                          """);
            var lib = cqlToolkit.MakeLibrary(cqlLibraryString.Cql);

            Assert.IsNotNull(lib.statements);
            Assert.AreEqual(1, lib.statements.Length);

            var queryExpression = lib.statements[0].expression;
            Assert.IsNotNull(queryExpression);
            Assert.IsInstanceOfType(queryExpression, typeof(Query));

            var query = (Query)queryExpression;
            Assert.IsNotNull(query.source);
            Assert.AreEqual(1, query.source.Length);

            var aliasedQuerySource = query.source[0];
            Assert.IsNotNull(aliasedQuerySource.expression);
            Assert.IsInstanceOfType(aliasedQuerySource.expression, typeof(Retrieve));

            var retrieve = (Retrieve)aliasedQuerySource.expression;
            Assert.AreEqual("{http://hl7.org/fhir}Observation", retrieve.dataType?.Name);
            // The list names no code path and no comparator, so both come from the model and the
            // default: the primary code path of Observation, matched with `in`.
            Assert.AreEqual("code", retrieve.codeProperty);
            Assert.AreEqual("in", retrieve.codeComparator);
            Assert.IsNotNull(retrieve.codes);
            Assert.IsInstanceOfType(retrieve.codes, typeof(Elm.List));

            var list = (Elm.List)retrieve.codes;
            Assert.IsNotNull(list.element);
            Assert.AreEqual(1, list.element.Length);
            Assert.IsInstanceOfType(list.element[0], typeof(CodeRef));

            var codeRef = (CodeRef)list.element[0];
            Assert.AreEqual("Systolic BP", codeRef.name);
        }

        [TestMethod]
        public void Retrieve_ValueSetWithoutCodePath_CarriesThePrimaryCodePath()
        {
            // `[Condition: "terminology"]` names no code path and no comparator. The ELM must still
            // say what to match against: the specification states that ELM is processable without
            // reference to the model information, and a Retrieve carrying `codes` and no
            // `codeProperty` is not, because the property lives only in the ModelInfo. This
            // translated with both attributes absent.
            var lib = CreateCqlToolkit().MakeLibrary("""
                library Test version '1.0.0'
                using FHIR version '4.0.1'
                valueset "terminology": 'http://fire.ly/ValueSet/Test'
                define "Conditions": [Condition: "terminology"]
                """);
            var retrieve = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Retrieve>();
            retrieve.codeProperty.Should().Be("code");
            retrieve.codeComparator.Should().Be("in");
            retrieve.codes.Should().BeOfType<ValueSetRef>();

            // Round-trip, because the attributes are what a consumer reads and the writer omits
            // unset ones silently.
            var reread = Hl7.Cql.Elm.Library.ParseFromJson(lib.SerializeToJson());
            var rereadRetrieve = reread.statements.Should().ContainSingle()
                                       .Which.expression.Should().BeOfType<Retrieve>().Subject;
            rereadRetrieve.codeProperty.Should().Be("code");
            rereadRetrieve.codeComparator.Should().Be("in");
        }

        [TestMethod]
        public void Retrieve_PrimaryCodePathIsTheModelsNotTheLiteralCode()
        {
            // The primary code path is a property of the type, not the string "code". A single
            // code against a type whose primary code path is something else used to be emitted
            // with `codeProperty = "code"` regardless, which names a property MedicationRequest
            // does not have.
            var lib = CreateCqlToolkit().MakeLibrary("""
                library Test version '1.0.0'
                using FHIR version '4.0.1'
                codesystem "RxNorm": 'http://www.nlm.nih.gov/research/umls/rxnorm'
                code "Metformin": '6809' from "RxNorm"
                valueset "Statins": 'http://fire.ly/ValueSet/Statins'
                define "By code": [MedicationRequest: "Metformin"]
                define "By value set": [MedicationRequest: "Statins"]
                define "Immunizations": [Immunization: "Statins"]
                define "Encounters": [Encounter: "Statins"]
                """);
            var byName = lib.statements.ToDictionary(s => s.name, s => (Retrieve)s.expression);

            byName["By code"].codeProperty.Should().Be("medication");
            byName["By code"].codeComparator.Should().Be("~");
            byName["By code"].codes.Should().BeOfType<ToList>();

            byName["By value set"].codeProperty.Should().Be("medication");
            byName["By value set"].codeComparator.Should().Be("in");

            byName["Immunizations"].codeProperty.Should().Be("vaccineCode");
            byName["Encounters"].codeProperty.Should().Be("type");
        }

        [TestMethod]
        public void Retrieve_ExplicitCodePathAndComparatorAreKept()
        {
            // Authored values win over the defaults, exactly as before.
            var lib = CreateCqlToolkit().MakeLibrary("""
                library Test version '1.0.0'
                using FHIR version '4.0.1'
                valueset "terminology": 'http://fire.ly/ValueSet/Test'
                define "Observations": [Observation: category ~ "terminology"]
                """);
            var retrieve = lib.Should().BeACorrectlyInitializedLibraryWithStatementOfType<Retrieve>();
            retrieve.codeProperty.Should().Be("category");
            retrieve.codeComparator.Should().Be("~");
        }
    }
}
