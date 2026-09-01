# Proving the wall fires

Four projects exist only to be built and to fail. They are not
in `Marvel.slnx`, so `dotnet build` and `dotnet test` never see them;
`tools/godot-wall.sh` builds them one at a time and checks the verdict.

```
GodotSharp              a stub. AssemblyName GodotSharp, one class, no Godot.
Marvel.WallProbe.Middle references the stub. The hop that makes it transitive.
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

The stub is why this runs offline. Nothing here downloads the real `GodotSharp`
package, and nothing under `datasets/` or the build may require the network —
see AGENTS.md.
