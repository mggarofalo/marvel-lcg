extends SceneTree


func _initialize() -> void:
	_start.call_deferred()


func _start() -> void:
	var script := load("res://smoke/hosted_invitation_layout_smoke.gd") as Script
	if script == null:
		push_error("Hosted invitation layout driver could not be loaded")
		quit(1)
		return
	root.add_child(script.new())
