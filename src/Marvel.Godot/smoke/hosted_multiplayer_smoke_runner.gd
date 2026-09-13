extends SceneTree


func _initialize() -> void:
	_start.call_deferred()


func _start() -> void:
	print("HOSTED_MULTIPLAYER_SMOKE_RUNNER_READY")
	var packed := load("res://smoke/HostedMultiplayerSmoke.tscn") as PackedScene
	if packed == null:
		push_error("HostedMultiplayerSmoke.tscn could not be loaded")
		quit(1)
		return
	root.add_child(packed.instantiate())
