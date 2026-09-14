/*
 * Copyright (c) 2026, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

#nullable enable

using Hl7.Cql.Elm;

namespace Hl7.Cql.CqlToElm.Test
{
    using Expression = Hl7.Cql.Elm.Expression;

    /// <summary>
    /// Covers <see cref="MessageProvider"/>'s tolerance of arguments whose result type could not be
    /// resolved.
    /// </summary>
    /// <remarks>
    /// An expression that fails validation keeps a null <c>resultTypeSpecifier</c>: the visitor that
    /// rejected it returns the partially built node with an error attached and never assigns a result
    /// type. Such a node can still be handed to a translator that is building the message describing
    /// that very failure, so every message formatter that renders argument types has to render the
    /// missing type rather than dereference it.
    /// </remarks>
    [TestClass]
    public class MessageProviderTest : Base
    {
        /// <summary>
        /// How a type that could not be resolved is rendered in a message. Deliberately restated here
        /// rather than read off the production constant, so that a change to the rendering has to be
        /// made twice and is therefore deliberate.
        /// </summary>
        private const string MissingTypeName = "(missing)";

        /// <summary>
        /// An expression node in the state a rejected literal is left in: no result type was ever
        /// assigned, so <c>resultTypeSpecifier</c> is null.
        /// </summary>
        private static Expression UntypedExpression() => new Literal { value = "2147483648" };

        private static Expression TypedExpression(TypeSpecifier type) =>
            new Literal { value = "1" }.WithResultType(type);

        [TestMethod]
        public void CouldNotResolveFunction_UntypedArgument_RendersMissingType()
        {
            var messageProvider = CreateCqlToolkit().GetMessageProvider();

            var message = messageProvider.CouldNotResolveFunction("Floor", UntypedExpression());

            message.Should().Contain("Floor").And.Contain(MissingTypeName);
        }

        [TestMethod]
        public void CouldNotResolveFunction_MixOfResolvedAndUntypedArguments_RendersBoth()
        {
            var messageProvider = CreateCqlToolkit().GetMessageProvider();

            var message = messageProvider.CouldNotResolveFunction(
                "Add", TypedExpression(SystemTypes.IntegerType), UntypedExpression());

            message.Should()
                   .Contain($"{SystemTypes.IntegerType}, {MissingTypeName}");
        }

        /// <summary>
        /// The <see cref="TypeSpecifier"/> overload has callers that pass an expression's
        /// <c>resultTypeSpecifier</c> straight through, so it can be handed the same null.
        /// </summary>
        [TestMethod]
        public void CouldNotResolveFunction_NullTypeSpecifier_RendersMissingType()
        {
            var messageProvider = CreateCqlToolkit().GetMessageProvider();

            var message = messageProvider.CouldNotResolveFunction("Start", (TypeSpecifier?)null);

            message.Should().Contain("Start").And.Contain(MissingTypeName);
        }

        [TestMethod]
        public void CallIsAmbiguous_UntypedArgument_RendersMissingType()
        {
            var messageProvider = CreateCqlToolkit().GetMessageProvider();

            var message = messageProvider.CallIsAmbiguous("Floor", [UntypedExpression()], []);

            message.Should().Contain("Floor").And.Contain(MissingTypeName);
        }

        /// <summary>
        /// End-to-end guard. An integer literal outside the range of the CQL <c>Integer</c> type
        /// (-2^31..2^31-1, CQL 1.5.3 Developer's Guide, "System-Defined Types", table 3-G) is rejected by
        /// literal validation and reaches the surrounding call carrying that error. Translation must
        /// report it rather than throw: before the formatters rendered a missing type, this expression
        /// raised a <see cref="NullReferenceException"/> and the caller saw no error at all.
        /// </summary>
        /// <remarks>
        /// Asserts that an error is reported, deliberately not how many. Whether the surrounding call
        /// also fails to resolve depends on whether a rejected literal carries a result type, which is a
        /// property of literal validation rather than of message formatting. Pinning the exact set here
        /// would make this test fail when that unrelated behaviour changes, while testing nothing extra
        /// about the formatters — those are covered directly above, on hand-built untyped nodes.
        /// </remarks>
        [TestMethod]
        public void OutOfRangeIntegerLiteralAsArgument_ReportsAnErrorInsteadOfThrowing()
        {
            var source = CqlLibraryString.Parse(
                """
                library "OutOfRangeIntegerLiteralAsArgument" version '1.0.0'

                define private "actual": Floor(2147483648)
                """);

            var translate = () => CreateCqlToolkit()
                                  .AddCqlLibraries([source])
                                  .TranslateToElm()
                                  .ArtifactsById[source]
                                  .ResultElmLibrary!;

            var library = translate.Should().NotThrow().Which;

            library.GetErrors().Select(e => e.message)
                   .Should().Contain("Unparseable numeric literal '2147483648'.");
        }
    }
}
