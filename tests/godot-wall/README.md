# Proving the wall fires

Four verdict projects exist only to be built by this script, with two support
projects supplying the transitive reference. They are not in `Marvel.slnx`, so
`dotnet build` and `dotnet test` never see them; `tools/godot-wall.sh` builds
the verdicts one at a time and checks whether each must pass or fail.

```
GodotSharp              a stub. AssemblyName GodotSharp, one class, no Godot.
Marvel.WallProbe.Middle references the stub. The hop that makes it transitive.
Marvel.WallProbe.OptOut sets the opt-out below the wall -> must FAIL (MARVELWALLOPT)
Marvel.WallProbe        references only Middle  -> must FAIL  (MARVELWALL)
Marvel.WallProbe.Allowed references the stub, opted out -> must SUCCEED
Marvel.WallProbe.Future targets net10.0            -> must FAIL  (MARVELTFM)
```

`Marvel.WallProbe` is the case that matters. It never names `GodotSharp`
anywhere, so a gate that only read the `.csproj` files would pass it, and the
engine would be one innocuous package away from seeing `Time.GetTicksMsec()`.

`Marvel.WallProbe.Allowed` matters for the opposite reason: a gate that cannot
be opted out of would make `Marvel.Godot` itself unbuildable, so the escape
hatch is load-bearing and is tested rather than assumed.

`Marvel.WallProbe.OptOut` proves that the escape hatch belongs only to the
actual Godot presentation assembly and its tests. A rules or content project
cannot make a new Godot reference legal by setting one property.

The stub is why this runs offline. Nothing here downloads the real `GodotSharp`
package, and nothing under `datasets/` or the build may require the network —
see AGENTS.md.
