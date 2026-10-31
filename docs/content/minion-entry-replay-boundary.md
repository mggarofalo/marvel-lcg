# Minion entry replay boundary

Replay v26 names the Core timing change established by the Thor engagement
prerequisite: real authored Hawkeye and Vulture reach the optional entry response
after Quickstrike damage, for reveal and non-reveal entry. The baseline
`7e352dbc` reaches that response before damage. The timing correction `64448751`
has independent review `67314c81`; see
[the prerequisite evidence](research/remaining/thor-engagement-prerequisite.md).
This compatibility packet changes no timing implementation, ability or setup data.

Saves under any earlier replay identity, including v25, are refused with
`save_incompatible` and left unchanged; there is no upgrade route (MARVEL-896).
