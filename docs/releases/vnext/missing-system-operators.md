## Fixes

- **CQL-to-ELM translator:** twelve CQL 1.5 system operators the runtime already implements, and
  whose ELM nodes the model already carries, are now declared in the System library so that a
  library using them translates: `ConvertsToBoolean`, `ConvertsToDate`, `ConvertsToDateTime`,
  `ConvertsToDecimal`, `ConvertsToInteger`, `ConvertsToLong`, `ConvertsToQuantity`,
  `ConvertsToString` and `ConvertsToTime` (each `(argument Any) Boolean`), `ConvertQuantity(argument
  Quantity, unit String) Quantity`, `CanConvertQuantity(argument Quantity, unit String) Boolean`,
  and `GeometricMean(argument List<Decimal>) Decimal`. Each previously failed with `Could not
  resolve call to operator ConvertsToInteger with signature (String).` and the like. `ToLong` gains
  its `Boolean` and `String` overloads beside the `Integer` one; the runtime converted all three.

- **Runtime:** the `ConvertsToX` predicates now test the value rather than the type. They answered
  from the argument's type alone, so `ConvertsToInteger('4.2')` and `ConvertsToBoolean('maybe')`
  were true; and they compared the boxed argument's type against the resolver's nullable types,
  which never match, so every value-typed argument answered false: `ConvertsToString(42)` was
  false. For a String the answer is now whether the corresponding conversion parses it; for a
  number, whether the value is representable (`ConvertsToBoolean(5)` is false, `ConvertsToInteger`
  of a Long checks the Integer range); a value already of the target type is true.
  **CQL evaluation results change** for these operators.
