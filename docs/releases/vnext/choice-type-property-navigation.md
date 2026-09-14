## Fixes

- A property path over a choice type now resolves instead of failing translation. The Developer's
  Guide (Choice Types) states that when accessing an element of a choice type with structured types as
  components, any element can be accessed, and that the result may itself be a choice type if the
  element is declared by several components with different types. The translator had no case for
  `ChoiceTypeSpecifier` when resolving a property path, so every such path was rejected with
  `Type Choice<...> has no members.`: for example `C.onset.value`, where `Condition.onset` is a choice
  of `dateTime | Age | Period | Range | string` in FHIR R4. The member's type is now the choice of its
  type over the components that declare it, collapsing to a single type when those agree. A component
  that does not declare the member contributes nothing and is null at run time, exactly as the implicit
  cast to a component type is.

- Property navigation no longer produces an expression without a result type. The rejected node above
  was left untyped, so passing it to an operator reached overload resolution with a null
  `resultTypeSpecifier` and translation threw `NullReferenceException` instead of returning an
  expression or reporting a translation error.

- A member that none of a choice type's components declares is now reported as
  `Member '<name>' not found for type Choice<...>.`, matching the wording already used for named types,
  rather than the categorical `Type Choice<...> has no members.`

These change CQL translation results: expressions that previously failed to translate now translate and
evaluate.
