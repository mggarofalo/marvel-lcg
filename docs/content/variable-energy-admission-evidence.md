# MARVEL-755 variable energy admission evidence

Payment admission packet based on `2f1c4693`, tracked by the MARVEL-755 parent
record `a71a1e88-2215-4512-b5f9-a7ef6cc9abbe`.

The engine checks effect admission against feasible future positive X values and,
when the body reads paid resource types, feasible declared receipts. It uses the
existing adjusted combined price and component allocation. Actual payment still
validates the selected generators, declarations, components and X before costs
commit. Both Action and interrupt/response admission use this payment context.

The synthetic tests use the existing villain query. This packet changes no
ability book, setup recipe, public product admission, version, or UI interaction.
Lightning Strike recipient selection and native product review are separate work.

## Final scoped verification

`dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`
passed with zero warnings and errors after all mutants were restored.

`dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~VariableEnergyAdmissionTests|FullyQualifiedName~VariableEventPayment'`
passed 38 cases on the final payment sources. This includes 20 admission cases
and 18 existing variable event payment/review cases.

Coverage includes adjusted printed prices of one and zero, separate printed/X
icons, typed and wild resources, feasible X bounds, conditional AERIAL damage,
Tough preservation/removal, payable zero-effect controls, a suspended resource
generator, chosen-X rejection and retry, window discovery and execution, and
query purity with RNG/pending-work baselines and an unprobed execution control.

Raw UTF-8 logs are outside the repository at `C:/Users/mggar/Source/`:

- `future-energy-final-restored-build.log`
- `future-energy-final-focused.log`

## Compiled mutation checks

All mutations compiled and failed for the intended behavioral observation:

| Mutation | Distinguishing failure | Log filename |
|---|---|---|
| Force proposed energy result to zero | 11 of 18 initial admission cases fail to find legal offers | `future-energy-mutant-result.log` |
| Skip chosen-X effect validation | Atomic rejection test reports no exception | `future-energy-mutant-validation.log` |
| Skip declared receipt requirements | Discounted wild incorrectly offers the physical-resource branch | `future-energy-mutant-receipt.log` |
| Use bare window offer admission | Both interrupt/response offers disappear | `future-energy-mutant-window-offer.log` |
| Use bare window initiation admission | Both valid interrupt/response payments cannot initiate | `future-energy-mutant-window-initiation.log` |

The final non-incremental build and 38-case run follow restoration of every
mutation. No mutant remains in source.

## Earlier regression and remaining integration gates

The complete Content test assembly passed 5,742 cases in 4m52s in
`future-energy-content.log`. That run predates the final two window tests and
the window discovery/initiation adapter changes. It is earlier regression
coverage, not a complete-suite green claim for the final packet. The integrated
candidate still needs its normal broader verification and independent review.

The normal pre-commit attempt is recorded in `future-energy-commit.log`. It
rejected the inherited `AbilityEffectLowering=301` source-layout finding; the
payment packet does not alter that class or bypass the hook. Its cohesive
correction belongs to the integrated delivery lead.

The cohesive lowering correction is integrated separately as prerequisite commit
`415ca1f2`. It moves the damage-family lowering responsibility without changing
the baseline. The payment commit's normal hook result is recorded separately in
`future-energy-final-commit.log`.
