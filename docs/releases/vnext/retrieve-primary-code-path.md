## Fixes

- **CQL-to-ELM translator:** a retrieve that filters by terminology without naming a code path,
  such as `[Condition: "Diabetes"]`, now carries the type's primary code path as `codeProperty`
  and, when no comparator is written, `in` (or `~` for a single code) as `codeComparator`. Both
  were previously left absent for every terminology except a single code, and for that one the path
  was the literal `"code"`, which is right for Observation and Condition and wrong for
  MedicationRequest (`medication`), Immunization (`vaccineCode`) and Encounter (`type`). The ELM
  specification states that ELM can be processed without reference to the model information; a
  Retrieve carrying `codes` and no `codeProperty` cannot be, because the property to match against
  lives only in the ModelInfo, so a consumer that does not carry the model either refuses the
  retrieve or guesses. The primary code path comes from the type's `ClassInfo`; a type that declares
  none leaves the attribute absent as before. Explicitly authored paths and comparators are kept.
  Emitted ELM gains these attributes; the SDK's own evaluation, which already resolved the primary
  code path at run time, is unchanged.
