# State digest v2

`World.Digest()` serializes the complete internal card state of one game.
`Marvel.Core.Digest.StateDigest` writes and fingerprints the canonical document.

The digest is a deterministic truth format for tests, replay diagnostics and
state comparison. It is not a client API. It includes hidden information and
must never cross the presentation or server boundary.

## Wire status

The digest is a wire format. These choices are fixed:

- version number and top-level shape;
- card allocation order and object ids;
- record key names and key order;
- zone names and suffixes;
- field names and field ordering;
- JSON escaping and whitespace; and
- SHA-256 spelling.

Changing one can invalidate every recorded state and every seeded game that
depends on card allocation. Update this document and the pinned tests before
changing the implementation.

## Document shape

The canonical document is:

```json
{"v":2,"cards":[]}
```

`cards` contains one record for every card object, sorted by ascending `id`.
Nothing is omitted because it is face down, out of play, a rules pseudo-card or
object id zero.

An empty world uses the complete empty document above. It is not the empty
string.

## Card record

Every record has 8 keys, all present and always written in this order:

| Key | Type | Meaning |
|---|---|---|
| `id` | integer | Card object id |
| `card` | string | Printed id of the current face |
| `zone` | string | `DeckType` name with an optional suffix |
| `owner` | integer | Current owner/controller field, or `-1` for the scenario |
| `index` | integer | Position in the area’s ordered list |
| `host` | integer | Host card id, or `-1` |
| `face_up` | boolean | Physical face-up state |
| `fields` | object | Named gameplay state |

Example:

```json
{"id":12,"card":"01001b","zone":"HeroArea","owner":0,"index":0,"host":-1,"face_up":true,"fields":{"ally_limit":3,"is_exhaust":0}}
```

## Object ids

Each world allocates card ids from zero in deterministic creation order. One
multi-face card has one object id. Ids are never reused within a world.

Setup creation order is specified in [setup-dataset.md](setup-dataset.md). A
change there changes this wire format even when every card ends in the same
area.

## Current face

`card` is the current printed face id, not the original face and not a title.
Changing identity form or advancing a multi-face scheme therefore changes this
field while preserving the object id.

## Zones and indices

`zone` is the ordinal `DeckType` name. The digest does not serialize area object
ids.

Two suffixes are reserved:

- `/removed` means the card is in that area’s removed list; and
- `/absent` means the card exists in the world but appears in no area list.

`/absent` is an invariant failure made visible. Digest construction emits it
with index `-1` instead of throwing and hiding the board that needs diagnosis.

`index` is zero-based within the selected ordinary or removed list. Recording
every index makes full deck and discard order observable.

The digest does not serialize play-area membership in game areas. That topology
is covered by direct rule tests and semantic events. Adding it would require a
new digest version, not another field silently appended to v2.

## Ownership and hosts

`owner` preserves the engine’s card ownership/control field. Scenario-owned
cards use `-1`.

Ownership, controller, related player and physical area are separate concepts in
the state model. Do not derive this value from a zone name.

`host` is the object id of the card that owns the current hosted area. It is
`-1` when the area has no host. Attachments and status cards therefore remain
associated with their host even when they carry no other changing field.

## Hidden state

`face_up` labels physical visibility. It does not suppress `card` or `fields`.

This is deliberate. A comparison format that omits hidden truth finds a
divergence only when the hidden card later becomes visible. The digest records
the divergence at the step it occurs.

`Marvel.View` produces a separate visibility-safe descriptor. The server never
serializes this digest.

## Fields

`fields` contains the named state returned by `StateFields.For` for every card in
every zone. An empty object means the card registers no fields. It never means
the digest skipped the card.

Keys use these namespaces:

| Prefix or key | Meaning |
|---|---|
| `is_exhaust` | Ready or exhausted state |
| `t_` | Printed or granted trait |
| `k_` | Token pool |
| `c_` | Counter pool |
| `f_` | Registered form marker |
| unprefixed names | Printed and live rule fields |

Zero-valued registered fields are emitted. Their presence makes the registered
key set part of the contract, so forgetting a field cannot pass because its
current value happened to be zero.

Printed constants remain in the digest. Named fields cannot collide by arithmetic
and the constants verify that both state construction and card facts agree.

Field keys are sorted with `StringComparer.Ordinal`. Culture-sensitive sorting
is forbidden.

`StateFields` refuses overlapping providers instead of choosing a winner. Two
sources claiming one field would otherwise make merge order silently decide the
wire value.

## Canonical JSON

`StateDigest.Canonical()` uses `Utf8JsonWriter` with:

```csharp
new JsonWriterOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    Indented = false,
}
```

The canonical spelling is the UTF-8 JSON emitted by that writer:

- no whitespace;
- top-level keys `v`, then `cards`;
- card keys in the 8-key table order;
- cards sorted by ascending id;
- fields sorted ordinally;
- decimal integers and lowercase JSON booleans; and
- platform-writer escaping under the relaxed encoder.

Non-ASCII characters remain literal UTF-8 where the writer permits them. JSON
control characters and required quoting use the writer’s exact escapes. No
normalization pass rewrites the output.

`StateDigestTests` pin apostrophes, non-ASCII text, control characters, quotes,
backslashes and Unicode edge cases. A round trip must reproduce the same bytes.

## Parsing and versions

`StateDigest.Parse` accepts version 2 and reconstructs card records. A different
version raises `NotSupportedException`.

Parsing exists for diagnostics and round-trip tests. It does not make alternate
key orders or escape spellings canonical; calling `Canonical()` writes the one
format above.

## Fingerprint

`Fingerprint()` is SHA-256 over the UTF-8 bytes of `Canonical()`, written as
lowercase hexadecimal.

The fingerprint is a compact identifier for the complete state. It does not
replace the canonical document when a mismatch needs a card-by-card explanation.

## Comparison

Canonical strings compare by byte equality. On mismatch, parse both documents
and report:

- missing or extra card ids;
- current face, zone, owner, index, host or face-up differences; and
- missing, extra or changed named fields.

Never ignore a mismatch by default. Any accepted difference must be an explicit,
named comparison policy outside the digest format.

## Validation

The digest tests hold:

- exact empty and populated canonical strings;
- fixed record and field ordering;
- one record for every world card;
- complete zone order, including removed lists;
- face, owner and host changes;
- out-of-play and zero-valued fields;
- strict version handling;
- canonical round trips; and
- SHA-256 fingerprints.

Gameplay tests add board-level invariants. Running the test suite must not create
or update a digest fixture in the repository.
