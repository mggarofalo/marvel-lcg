# C# source layout

Source layout is part of comprehensibility. A type should be discoverable from
its name, readable without reconstructing a mechanical split, and small enough
for an editor, a language server and a reviewer to present as one coherent
unit.

## Files and types

Every non-nested top-level type lives in its own file, and the basename of that
file is exactly the type name. This applies equally to public and internal
classes, records, structs, interfaces and enums.

Use directories to express a family of collaborators. Do not prefix a peer's
file with the type from which it was extracted: `Structural/SequenceFrame.cs`
contains `SequenceFrame`; `AbilityStructuralExecution.SequenceFrame.cs` does
not. A dotted `Owner.Concern.cs` name is reserved for a file containing a
partial declaration of `Owner`. Application partial types are prohibited, so
application source ordinarily has no such filenames.

Nested types are the deliberate exception. A small private implementation type
may stay in its owner because the compiler enforces that relationship. A nested
public type may stay when qualification is part of the model, as with a closed
algebra whose values are named through their containing type. When a nested
type grows into an independently understandable collaborator, make it internal
and give it its own matching file rather than weakening the file rule.

Generated code and declarations that a source generator requires to be partial
retain only the exact exceptions enforced by `.husky/csx/no-partial-types.csx`.

## Class size

A class declaration is never more than 500 lines, including all of its members
and nested types. Five hundred is an absolute failure boundary, not a design
target. A class over 300 lines is a large-class remediation finding. Existing
large classes live in the exact diagnostic baseline; a new one is rejected and
the baseline shrinks as existing classes are decomposed. A reviewed exception
must name the cohesive reason the type is clearer whole and remains subject to
the 500-line ceiling.

Approaching the boundary is evidence that the type may own several decisions,
state machines or infrastructure concerns. Prefer extracting collaborators
with names that state those responsibilities. Moving methods into a partial
declaration, compressing formatting, or collecting unrelated helpers under a
new generic name does not address the problem.

The existing 500-line file gate remains useful, but one type per file and the
ban on application partial types make class size the design constraint that
the file limit was intended to approximate.

## Parameter lists

A behavioural method or constructor takes no more than seven parameters. Like
the class-size boundary, seven is a failure threshold rather than a desirable
signature size.

When a signature reaches it, first ask whether the operation has more than one
responsibility. Decompose that responsibility when it does. Introduce a
parameter object only when the arguments form a real concept with invariants or
a useful lifetime of its own; a bag named `Options`, `Context` or `Data` merely
moves the same incomprehensibility elsewhere.

Data contracts and positional records describe a value rather than a
behavioural call. Review a wide contract for cohesion and wire compatibility,
but do not disguise its fields solely to satisfy the behavioural signature
limit.

## Deterministic enforcement

Enforce every syntactic rule with Roslyn and run that enforcement from both
`Marvel.Architecture.Tests` and a Husky pre-commit task. Do not maintain a
second regular-expression or token-based interpretation in the hook: one shared
syntax-aware policy must produce the same answer in both entry points. Editor
settings and formatters are conveniences, not authorities.

The check walks every Git-tracked or unignored application C# syntax tree in
ordinal path order and emits stable diagnostics as
`relative/path.cs:line: rule: declaration`.

The deterministic rules are:

- each application file has at most one non-nested top-level type;
- that type's identifier exactly matches the file basename;
- a class declaration over 300 lines is a baselined remediation finding and a
  declaration over 500 lines is never admitted;
- a behavioural method or constructor has at most seven parameters; and
- only the exact generated partial declarations already admitted by the
  partial-type policy are exempt from the applicable layout rules.

Keep existing violations in a sorted, committed baseline of complete diagnostic
keys, not only a count. A new or changed violation then fails even when another
violation was removed in the same change. Removing a violation requires removing
its baseline entry, so the baseline can only shrink during remediation. The test
prints both unadmitted violations and stale baseline entries.

Run the policy through the normal architecture test project so CI executes it on
both operating systems. A Husky task invokes the same checker before commit and
must propagate its exit code. Keep the existing whole-file length hook as a fast
additional signal; it is not a substitute for the syntax-aware class and type
checks.

```bash
dotnet test tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj -c Release
```

Type-name quality cannot be decided completely by syntax. Make its review
reproducible instead: produce an ordinal report of every top-level type with its
namespace, accessibility, filename, direct base types and reference count, plus
deterministic candidate flags for filename mismatch, mechanical owner prefix,
and agreed vague role words. Bare top-level `Data`, `Entry`, `Helper`, `Helpers`,
`Info`, `Manager`, `Misc`, `Record`, `Stuff`, `Utilities` and `Utility` names are
always candidates; a descriptive compound such as `HistoryEntryDescriptor` is
not. The final phase records a keep or rename disposition for every flagged
type. Completeness of that review is machine-checkable even though the judgment
is human.

## Remediation order

Clean the existing source in stages so file-only work remains distinguishable
from design and compatibility changes:

1. Add the shared Roslyn-backed policy, its architecture tests, its Husky
   pre-commit entry point and the exact diagnostic baseline described above.
   Cover top-level type and filename relationships, class length and behavioural
   parameter counts from the start. Admit current violations explicitly and
   remove each admission as it is fixed; new violations are rejected
   immediately.
2. Repair inverted anchors where `X.cs` contains a companion while `X.X.cs`
   contains `X`. Rename extracted peer files to the type they actually contain,
   and remove copied imports and orphaned comments.
3. Split public contract and presentation bundles one assembly or vocabulary at
   a time without renaming types or changing namespaces, signatures or wire
   representations.
4. Split internal execution algebras and state families into one matching file
   per type, using directories rather than filename prefixes to retain useful
   grouping.
5. Apply the same production rule to repository tools. Tests may use a
   behaviour-qualified filename for one test class, but do not colocate
   unrelated top-level test types.
6. Review each class over 300 lines and decompose its responsibilities, never
   admitting one over 500. Do this before signature and naming work so the real
   collaborators exist and can receive accurate names.
7. Review and remediate behavioural methods and constructors with more than
   seven parameters. Prefer responsibility decomposition; use a cohesive value
   type where the arguments genuinely travel together.
8. Generate the deterministic type-name review report, resolve every candidate,
   and review type names last. Rename vague, misleading, mechanically prefixed
   or responsibility-obscuring types only after their final boundaries are
   known.

The final naming phase is not presumed to be cosmetic. Before renaming a public
or persisted type, search serializers, source-generation declarations,
`nameof(...)`, event discriminators, save records, tests and documentation.
Where a type name contributes to a protocol, save or digest spelling, preserve
it or treat the rename as an explicit compatibility change under the relevant
contract documentation. Internal names can be improved directly once their
responsibility is stable.

Each structural batch must build and run its focused tests. Run the complete
suite after the final batch in an assembly or wire vocabulary. A file move
needs no new behavioural test; a decomposition that introduces a decision does.
