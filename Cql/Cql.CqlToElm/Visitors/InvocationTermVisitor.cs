/*
 * Copyright (c) 2025, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.CqlToElm.Grammar;
using Hl7.Cql.Elm;
using Hl7.Cql.Model;

namespace Hl7.Cql.CqlToElm.Visitors
{
    internal partial class ExpressionVisitor
    {
        // (qualifierExpression '.')* referentialIdentifier
        public override Expression VisitQualifiedIdentifierExpression([NotNull] cqlParser.QualifiedIdentifierExpressionContext context)
        {
            var qualifiers = context.qualifierExpression();
            Expression? expression = null;
            if (qualifiers.Length > 0)
            {
                var term = qualifiers[0].referentialIdentifier().Parse();
                if (LibraryBuilder.CurrentScope.TryResolveSymbol(term, out var symbol))
                    expression = symbol.ToRef(null);
                else return new IdentifierRef
                {
                    name = term,
                }.AddError(MessagingProvider.CouldNotResolveInCurrent(term));

                for(int i = 1; i < qualifiers.Length; i++)
                {
                    term = qualifiers[i].referentialIdentifier().Parse();
                    expression = navigateIntoType(expression, term);
                }
                var refId = context.referentialIdentifier().Parse();
                expression = navigateIntoType(expression, refId);
                return expression;
            }
            else
            {
                var term = context.referentialIdentifier().Parse();
                if (LibraryBuilder.CurrentScope.TryResolveSymbol(term, out var symbol))
                    return symbol.ToRef(null);
                else return new IdentifierRef
                {
                    name = term,
                }.AddError(MessagingProvider.CouldNotResolveInCurrent(term));
            }
        }

        public override Expression VisitTermExpression([NotNull] cqlParser.TermExpressionContext context)
        {
            var term = base.VisitTermExpression(context);

            if (term is UsingRef ur)
                return SymbolScopeExtensions.MakeErrorReference(null, ur.UsingDef.localIdentifier,
                    "A reference to a model library is unexpected at this point.").WithLocator(context.Locator());
            else if (term is IncludeRef ir)
                return ir.AddError(MessagingProvider.ExpressionCannotBeLibraryRef(ir.IncludeDef.localIdentifier))
                    .WithLocator(context.Locator());
            else if (term is null)
            {
                var message = $"Type {context.expressionTerm().GetType()} is not implemented";
                return new Message()
                {
                    source = new Null().WithResultType(SystemTypes.AnyType),
                    message = ElmFactory.Literal(message),
                }
                .AddError(message)
                .WithLocator(context.Locator())
                .WithResultType(SystemTypes.AnyType);
            }
            else return term;
        }

        public override Expression VisitInstanceSelector([NotNull] cqlParser.InstanceSelectorContext context)
        {
            var type = TypeSpecifierVisitor.Visit(context.namedTypeSpecifier());
            if (type is Elm.NamedTypeSpecifier namedType)
            {
                var modelType = ModelProvider.FindTypeInfoByNamedType(namedType);
                if (modelType != null)
                {
                    if (modelType.Type is ClassInfo @class)
                    {
                        var instanceElementContexts = context.instanceElementSelector();
                        var instanceElements = new InstanceElement[instanceElementContexts.Length];
                        for (int i = 0; i < instanceElementContexts.Length; i++)
                        {
                            var elementName = instanceElementContexts[i].referentialIdentifier().Parse();
                            if (!ModelProvider.TryGetElement(@class, elementName, out var classElement) || classElement is null)
                            {
                                return new Instance()
                                    .AddError($"Member {elementName} not found for type {namedType.name.Name}.")
                                    .WithLocator(context.Locator())
                                    .WithResultType(type);
                            }
                            else
                            {
                                Elm.TypeSpecifier elementType;
                                if (classElement.typeSpecifier is not null)
                                    elementType = classElement.typeSpecifier.ToElm(ModelProvider);
                                else if (classElement.type is not null)
                                {
                                    if (ModelProvider.TryGetTypeSpecifierForQualifiedName(classElement.type, out var ent))
                                        elementType = ent!;
                                    else
                                        return new Instance()
                                            .AddError($"The named type '{classElement.type}' for element {elementName} could not be resolved to any model type.")
                                            .WithLocator(context.Locator())
                                            .WithResultType(type);
                                }
                                else if (classElement.elementTypeSpecifier is Model.ListTypeSpecifier { } listTypeSpecifier)
                                {
                                    if (ModelProvider.TryGetTypeSpecifierForQualifiedName(listTypeSpecifier.elementType, out var elt))
                                        elementType = elt.ToListType();
                                    else
                                        return new Instance()
                                            .AddError($"The named type '{listTypeSpecifier.elementType}' for element {elementName} could not be resolved to any model type.")
                                            .WithLocator(context.Locator())
                                            .WithResultType(type);
                                }
                                else if (classElement.elementTypeSpecifier is Model.ChoiceTypeSpecifier { } choiceTypeSpecifier)
                                {
                                    var ct = choiceTypeSpecifier.choice.Select(c => c.ToElm(ModelProvider)).ToArray();
                                    elementType = new Elm.ChoiceTypeSpecifier { choice = ct };
                                }
                                else if (classElement.elementType is not null)
                                {
                                    if (ModelProvider.TryGetTypeSpecifierForQualifiedName(classElement.elementType, out var ent))
                                        elementType = ent!;
                                    else
                                        return new Instance()
                                            .AddError($"The named type '{classElement.elementType}' for element {elementName} could not be resolved to any model type.")
                                            .WithLocator(context.Locator())
                                            .WithResultType(type);
                                }
                                else
                                {
                                    return new Instance()
                                        .AddError($"Element {elementName} does not have any declared type.")
                                        .WithLocator(context.Locator())
                                        .WithResultType(type);
                                }
                                var elementExpression = Visit(instanceElementContexts[i]);
                                var conversionResult = CoercionProvider.Coerce(elementExpression, elementType);
                                if (conversionResult.Success)
                                {
                                    var instanceElement = new InstanceElement()
                                    {
                                        name = elementName,
                                        value = conversionResult.Result,
                                    };
                                    instanceElements[i] = instanceElement;
                                }
                                else
                                {
                                    // CoercionResult only carries a cost, so the diagnostic is composed here.
                                    return new Instance()
                                        .AddError($"The value for element {elementName} of type '{elementExpression.resultTypeSpecifier}' cannot be converted to the declared type '{elementType}'.")
                                        .WithLocator(context.Locator())
                                        .WithResultType(type);
                                }
                            }
                        }
                        var instance = new Instance
                        {
                            element = instanceElements,
                            classType = namedType.name,
                        };
                        return instance
                            .WithId()
                            .WithLocator(context.Locator())
                            .WithResultType(namedType);
                    }
                    else return new Instance()
                        .AddError($"The named type '{namedType.name.Name}' is not a class type.")
                        .WithLocator(context.Locator())
                        .WithResultType(type);
                }
                else return new Instance()
                    .AddError($"The named type '{namedType.name.Name}' could not be resolved to any model type.")
                    .WithLocator(context.Locator())
                    .WithResultType(type);
            }
            else return new Instance()
                .AddError("A named type is required in this context.")
                .WithLocator(context.Locator())
                .WithResultType(type);
        }

        // expressionTerm '.' qualifiedInvocation
        public override Expression VisitInvocationExpressionTerm([NotNull] cqlParser.InvocationExpressionTermContext context)
        {
            if (context.expressionTerm()?.GetText() == "\"EOB\"")
            {

            }
            var left = Visit(context.expressionTerm());
            if (left is UsingRef ur)
                return SymbolScopeExtensions.MakeErrorReference(null, ur.UsingDef.localIdentifier,
                    "A reference to a type is unexpected at this point.").WithLocator(context.Locator());

            var invocation = context.GetChild(2) switch
            {
                cqlParser.QualifiedMemberInvocationContext memberCtx => memberInvocation(memberCtx),
                cqlParser.QualifiedFunctionInvocationContext functionCtx => functionInvocation(functionCtx.qualifiedFunction()),
                _ => throw new InvalidOperationException($"Unexpected token following '.' in rule expressionTerm")
            };
            return invocation;

            Expression memberInvocation(cqlParser.QualifiedMemberInvocationContext memberCtx)
            {
                var memberName = memberCtx.referentialIdentifier().Parse();
                if (left is IncludeRef ir)
                {
                    var libraryName = ir.IncludeDef.localIdentifier;
                    return LibraryBuilder.CurrentScope
                                         .Ref(MessagingProvider, libraryName, memberName)
                                         .WithLocator(context.Locator());
                }
                else
                {
                    return navigateIntoType(left, memberName)
                        .WithLocator(context.Locator());
                }
            }
            Expression functionInvocation(cqlParser.QualifiedFunctionContext functionCtx)
            {
                var locator = functionCtx.Locator();
                var functionName = functionCtx.identifierOrFunctionIdentifier().Parse();
                var @params = functionCtx.paramList()?.expression().Select(Visit)?.ToArray() ?? Array.Empty<Expression>();
                var invocation = left switch
                {
                    IncludeRef ir => createFunctionInvocation(ir.IncludeDef.localIdentifier, functionName, @params, fluent: false, locator),
                    { } => createFunctionInvocation(null, functionName, new[] { left }.Concat(@params).ToArray(), fluent: true, locator),
                    _ => throw new InvalidOperationException($"Left side of invocation expression term is null.")
                };
                return invocation;
            }
        }

        private Expression navigateIntoType(Expression source, string memberName)
        {
            return source.resultTypeSpecifier switch
            {
                Elm.NamedTypeSpecifier nts => navigateIntoNamedType(source, nts, memberName),
                Elm.TupleTypeSpecifier tts => navigateIntoTuple(source, tts, memberName),
                Elm.IntervalTypeSpecifier ivs => navigateIntoInterval(source, ivs, memberName),
                Elm.ListTypeSpecifier lts => navigateIntoList(source, lts, memberName),
                Elm.ChoiceTypeSpecifier cts => navigateIntoChoice(source, cts, memberName),
                // Every other branch produces a typed node, so this one must too: an expression that
                // escapes translation without a result type makes every consumer of resultTypeSpecifier
                // (overload resolution, coercion, error formatting) a null-reference hazard.
                _ => makeProp(source, memberName)
                        .AddError($"Type {source.resultTypeSpecifier} has no members.")
                        .WithResultType(SystemTypes.AnyType)
            };
        }

        private Expression navigateIntoList(Expression source, Elm.ListTypeSpecifier lts, string memberName)
        {
            if (lts is null)
            {
                throw new ArgumentNullException(nameof(lts));
            }
            // If we navigate into a list, say, Patient.name.family, we actually need to generate a
            // query. In this query, Patient.name is the source, and "family" is the property we need to
            // navigate into:
            //
            //    from Patient.name $this
            //    where $this.name is not null
            //    select $this.name
            //
            var aliasRef = new AliasRef { name = "$this" }.WithResultType(lts.elementType);
            var prop = navigateIntoType(aliasRef, memberName);

            var q = new Query()
            {
                source = new[] { new AliasedQuerySource()
                    {
                        expression = source,
                        alias = "$this"
                    }.WithResultType(lts) },
                where = new Not
                {
                    operand = new IsNull
                    {
                        operand = prop
                    }.WithResultType(SystemTypes.BooleanType)
                }.WithResultType(SystemTypes.BooleanType),
                @return = new ReturnClause()
                {
                    expression = prop,
                    distinct = false
                }
            }.WithResultType(prop.resultTypeSpecifier.ToListType());

            // If the result itself is again a list (as in Patient.name.given), then the FhirPath
            // logic requires us to flatten that list.
            return prop.resultTypeSpecifier switch
            {
                Elm.ListTypeSpecifier qlts => new Flatten { operand = q }.WithResultType(qlts),
                _ => q
            };
        }

        // https://build.fhir.org/ig/HL7/cql/04-logicalspecification.html#property
        // Property expressions can also be used to access the individual points and closed indicators
        // for interval types using the property names low, high, lowClosed, and highClosed.
        private Property navigateIntoInterval(Expression source, Elm.IntervalTypeSpecifier ivs, string memberName)
        {
            var prop = makeProp(source, memberName).WithResultType(ivs.pointType);

            return memberName switch
            {
                "low" or "high" => prop.WithResultType(ivs.pointType),
                "lowClosed" or "highClosed" => prop.WithResultType(SystemTypes.BooleanType),
                _ => prop.AddError($"Invalid interval property name '{memberName}'.")
            };
        }

        // https://cql.hl7.org/03-developersguide.html#choice-types
        // "When accessing an element of a choice type with structured types as components, any element
        // can be accessed. Note, however, that if the element being accessed is present in multiple
        // components, the resulting expression may be a choice type if the elements have different types."
        //
        // So the member is legal as long as at least one component declares it, and the type of the
        // resulting property is the choice of that member's type over the components that declare it.
        // Components that do not declare the member simply contribute nothing: at run time they yield
        // null, exactly as the implicit cast to a component type does.
        private Property navigateIntoChoice(Expression source, Elm.ChoiceTypeSpecifier cts, string memberName)
        {
            var prop = makeProp(source, memberName);

            if (!tryGetMemberType(cts, memberName, out var memberType) || memberType is null)
                return prop
                    .AddError($"Member '{memberName}' not found for type {cts}.")
                    .WithResultType(SystemTypes.AnyType);

            return prop.WithResultType(memberType);
        }

        /// <summary>
        /// Determines the type of <paramref name="memberName"/> on <paramref name="typeSpecifier"/>, without
        /// building an expression for it. Used to resolve a member across the components of a choice type.
        /// </summary>
        /// <remarks>
        /// A list-typed component yields <c>false</c>: navigating into a list is a query rather than a
        /// property access (see <see cref="navigateIntoList"/>), which cannot be folded into the single
        /// <see cref="Property"/> that navigating into a choice type produces.
        /// </remarks>
        /// <returns><c>true</c> if <paramref name="typeSpecifier"/> has such a member, <c>false</c> otherwise.</returns>
        private bool tryGetMemberType(Elm.TypeSpecifier typeSpecifier, string memberName, out Elm.TypeSpecifier? memberType)
        {
            switch (typeSpecifier)
            {
                case Elm.NamedTypeSpecifier nts:
                    {
                        var (uri, typeName) = nts;
                        // Unlike FindTypeInfoByNamedType, this must not throw for a component whose type is
                        // not in any loaded model - that component just cannot contribute a member here.
                        if (ModelProvider.TryFindTypeInfoByName(uri, typeName, out var typeInfo, out _)
                            && typeInfo is ClassInfo ci
                            && ModelProvider.TryGetElement(ci, memberName, out var elementInfo)
                            && elementInfo is not null)
                        {
                            memberType = elementInfo.GetTypeSpecifierForElement(ModelProvider);
                            return true;
                        }
                        break;
                    }
                case Elm.TupleTypeSpecifier tts:
                    {
                        if (tts.element?.SingleOrDefault(e => e.name == memberName) is { } element)
                        {
                            memberType = element.elementType;
                            return true;
                        }
                        break;
                    }
                case Elm.IntervalTypeSpecifier ivs:
                    switch (memberName)
                    {
                        case "low" or "high":
                            memberType = ivs.pointType;
                            return true;
                        case "lowClosed" or "highClosed":
                            memberType = SystemTypes.BooleanType;
                            return true;
                    }
                    break;
                case Elm.ChoiceTypeSpecifier cts:
                    {
                        // The member's type is the choice of its type over every component that declares it.
                        // Components contributing the same type are collapsed, and a choice of one degenerates
                        // to that single type.
                        var memberTypes = new List<Elm.TypeSpecifier>();
                        foreach (var alternative in cts.choice ?? Enumerable.Empty<Elm.TypeSpecifier>())
                        {
                            if (alternative is not null
                                && tryGetMemberType(alternative, memberName, out var alternativeMemberType)
                                && alternativeMemberType is not null
                                && !memberTypes.Contains(alternativeMemberType))
                            {
                                memberTypes.Add(alternativeMemberType);
                            }
                        }

                        if (memberTypes.Count > 0)
                        {
                            memberType = memberTypes.Count == 1
                                ? memberTypes[0]
                                : new Elm.ChoiceTypeSpecifier(memberTypes);
                            return true;
                        }
                        break;
                    }
            }

            memberType = null;
            return false;
        }

        private Property makeProp(Expression source, string member) => new()
        {
            source = source,
            path = member,
            scope = null  // Don't know what this is for
        };

        private Property navigateIntoTuple(Expression source, Elm.TupleTypeSpecifier tts, string memberName)
        {
            var targetElement = tts.element.Where(e => e.name == memberName).SingleOrDefault();
            var prop = makeProp(source, memberName);

            if (targetElement is not null)
            {
                prop.WithResultType(targetElement.elementType);
            }
            else
            {
                prop.AddError($"Member '{memberName}' not found in tuple.");
                prop.WithResultType(SystemTypes.AnyType);
            }

            return prop;
        }

        private Property navigateIntoNamedType(Expression source, Elm.NamedTypeSpecifier nts, string memberName)
        {
            var (_, type) = ModelProvider.FindTypeInfoByNamedType(nts);

            var prop = makeProp(source, memberName);

            if (type is ClassInfo ci && ModelProvider.TryGetElement(ci, memberName, out var elementInfo))
            {
                return prop.WithResultType(elementInfo!.GetTypeSpecifierForElement(ModelProvider));
            }
            else
            {
                return prop
                    .AddError($"Member '{memberName}' not found for type {nts}.")
                    .WithResultType(SystemTypes.AnyType);
            }
        }



        // : referentialIdentifier             #memberInvocation
        public override Expression VisitMemberInvocation([NotNull] cqlParser.MemberInvocationContext context)
        {
            var identifier = context.referentialIdentifier().Parse();
            if (!LibraryBuilder.CurrentScope.TryResolveSymbol(null, identifier, out var def))
            {
                if (LibraryBuilder.CurrentScope.TryResolveSymbol(null, "$this", out var @this))
                {
                    var propertyExpression = navigateIntoType(@this!.ToRef(null), identifier);
                    // in a query context, when a property doesn't exist on $this, a could not resolve
                    // error is issued instead of a "member not found" error.
                    if (@this.Name == "$this" && propertyExpression.GetErrors()?.Length > 0)
                        return new AliasRef { name = identifier }
                            .AddError(MessagingProvider.CouldNotResolveInCurrent(identifier))
                            .WithLocator(context.Locator())
                            .WithResultType(SystemTypes.AnyType);
                    else
                        return propertyExpression;
                }
            }
            var @ref = LibraryBuilder.CurrentScope!
                                     .Ref(MessagingProvider, null, identifier)
                                     .WithLocator(context.Locator());
            if (def is Element element)
            {
                var errors = element.GetErrors()
                    .OfType<CircularReferenceError>()
                    .ToArray();
                foreach (var error in errors)
                    @ref.AddError(error);
            }
            return @ref;
        }

        // function  : referentialIdentifier '(' paramList? ')'
        public override Expression VisitFunction([NotNull] cqlParser.FunctionContext context)
        {
            var funcName = context.referentialIdentifier().Parse();
            var paramList = ParseParamList(context.paramList());
            return createFunctionInvocation(null, funcName, paramList, fluent: false, context.Locator());
        }

        private Expression createFunctionInvocation(string? libraryName, string funcName, Expression[] paramList, bool fluent, string locator)
        {
            IFunctionElement? symbolDef = null;
            // in some contexts, e.g. query sort statements like this:
            //
            // define fluent function foo(i Integer): i
            // define q: ({ 2,1}) sL sort by foo() asc
            //
            // we have to test the existence of $this.foo() first.
            // If it exists, it takes precedence over other matching non-fluent functions (for some reason).
            if (LibraryBuilder.CurrentScope.TryResolveSymbol("$this", out var @this))
            {
                if (LibraryBuilder.CurrentScope.TryResolveFluentFunction(funcName, out symbolDef))
                {
                    var fluentParams = new Expression[] { @this.ToRef(null) }.Concat(paramList).ToArray();
                    var result = symbolDef switch
                    {
                        IHasSignature ihs        => InvocationBuilder.MatchSignature(ihs, fluentParams),
                        OverloadedFunctionDef ov => InvocationBuilder.MatchSignature(ov, fluentParams),
                        _                        => null
                    };
                    if (result?.Compatible ?? false)
                        return invoke(result.Function, null, fluentParams, true);
                }
            }
            if (fluent)
            {
                if (!LibraryBuilder.CurrentScope.TryResolveFluentFunction(funcName, out symbolDef))
                    return unresolved();
            }
            else if (libraryName is null)
            {
                if (!LibraryBuilder.CurrentScope.TryResolveFunction(funcName, out symbolDef))
                    return unresolved();

            }
            else if (libraryName is not null)
            {
                if (LibraryBuilder.CurrentScope.TryResolveSymbol(libraryName, out var usingSymbol))
                {
                    if (usingSymbol is ReferencedLibrary refLib)
                    {
                        if (!refLib.Symbols.TryResolveFunction(funcName, out symbolDef))
                            return unresolved();
                    }
                    else
                        return unresolved();
                }
            }
            return invoke(symbolDef, libraryName, paramList, fluent);
            FunctionRef unresolved() => new FunctionRef { name = funcName, operand = paramList }
                    .WithResultType(SystemTypes.AnyType)
                    .WithLocator(locator)
                    .AddError(MessagingProvider.CouldNotResolveFunction(funcName, paramList));
            Expression initializeFunctionRef(FunctionDef funcDef, string? libraryName, Expression[] arguments, bool fluent)
            {
                var funcRef = InvocationBuilder.Invoke(funcDef, libraryName, arguments);
                if (funcRef is FunctionRef fr)
                    fr.libraryName = libraryName;
                if (fluent && !funcDef.fluent)
                    funcRef.AddError($"Function '{funcDef.name}' is called fluently, but its definition is not marked as fluent.");
                if (funcRef.resultTypeSpecifier is null)
                    throw new InvalidOperationException($"Missing result type specifier");
                return funcRef.WithLocator(locator);
            }
            Expression initializeOverload(OverloadedFunctionDef function, string? libraryName, Expression[] arguments, bool fluent)
            {
                var result = InvocationBuilder.MatchSignature(function, arguments);
                if (result.Compatible)
                {
                    var expression = InvocationBuilder.Invoke(result.Function, libraryName, arguments);
                    if (fluent && !result.Function.Fluent)
                        expression.AddError($"Function '{result.Function.Name}' is called fluently, but its definition is not marked as fluent.");
                    return expression.WithLocator(locator);
                }
                else
                    return new FunctionRef()
                    {
                        name = function.Name,
                        libraryName = libraryName,
                    }
                    .WithResultType(SystemTypes.AnyType)
                    .WithLocator(locator)
                    .AddError(result.Error() ?? throw new InvalidOperationException($"No compatible overload was found, but no error message was populated."));
            }

            Expression invoke(IFunctionElement? symbolDef, string? libraryName, Expression[] paramList, bool fluent)
            {
                return symbolDef switch
                {
                    OverloadedFunctionDef overloadedDef => initializeOverload(overloadedDef, libraryName, paramList, fluent),
                    FunctionDef function => initializeFunctionRef(function, libraryName, paramList, fluent),
                    DeferredFunctionDef dfd => initializeFunctionRef(dfd.Resolve(), libraryName, paramList, fluent),
                    _ => unresolved()
                };
            }
        }





        // paramList : expression(',' expression)*
        public Expression[] ParseParamList(cqlParser.ParamListContext? context) => context?.expression().Select(Visit).ToArray()
            ?? Array.Empty<Expression>();


        // | '$this'                           #thisInvocation
        public override Expression VisitThisInvocation([NotNull] cqlParser.ThisInvocationContext context) =>
            LibraryBuilder.CurrentScope
                          .Ref(MessagingProvider, null, context.GetText())
                          .WithLocator(context.Locator());

        // | '$index'                          #indexInvocation
        public override Expression VisitIndexInvocation([NotNull] cqlParser.IndexInvocationContext context) =>
            LibraryBuilder.CurrentScope
                          .Ref(MessagingProvider, null, context.GetText())
                          .WithLocator(context.Locator());

        // | '$total'                          #totalInvocation
        public override Expression VisitTotalInvocation([NotNull] cqlParser.TotalInvocationContext context) =>
            LibraryBuilder.CurrentScope
                          .Ref(MessagingProvider, null, context.GetText())
                          .WithLocator(context.Locator());
    }

}
