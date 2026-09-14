## Fixes

- **ELM JSON serialization:** an optional ELM property that was explicitly set is no longer dropped
  just because its value happens to equal the CLR default for its type. The visible case is a
  `Quantity` of zero: `0'cm' > 1'cm'` serialized its left operand as
  `{"type":"Quantity","unit":"cm"}` — `value` absent rather than `0` — which is silent corruption
  rather than a formatting quirk, because ELM makes `Quantity.value` optional and a reader therefore
  cannot tell that node apart from a quantity that never had a value. A consumer either evaluates the
  expression against null, answering a different question than the CQL asked, or rejects the node.
  The same loss applied to `Ratio.numerator`/`Ratio.denominator` and to every other property paired
  with an `xxxSpecified` flag — an explicit `"external": false` or `"fluent": false`, a `startLine` of
  zero — including on a plain read-then-write round trip, since the flag is set from the property's
  presence in the source JSON. The serializer was applying two independent omission rules to those
  properties and requiring both to pass: the `xxxSpecified` flag, and a general "skip any value equal
  to the type default" rule. For a property carrying an explicit presence flag those are not
  independent questions — "absent" and "present, and equal to the default" are different statements
  about the ELM, and only the flag can distinguish them — so the flag now decides alone. Properties
  with no such flag are unchanged, and a property whose flag is `false` is still omitted. Emitted ELM
  JSON therefore gains these properties where they were previously lost; nothing that was already
  being written stops being written.
