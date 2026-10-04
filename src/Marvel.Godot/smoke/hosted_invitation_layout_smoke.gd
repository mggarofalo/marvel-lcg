extends "res://smoke/hosted_multiplayer_smoke.gd"

const InvitationLayout = preload("res://smoke/hosted_invitation_layout_checks.gd")


func _run() -> void:
	var packed := load("res://Main.tscn") as PackedScene
	if packed == null:
		_fail("Main.tscn could not be loaded")
		return
	if not await _open_host(packed):
		return
	if not await InvitationLayout.check(self):
		return
	print("HOSTED_INVITATION_LAYOUT_SMOKE_OK")
	_finish(0)


func _new_client_viewport() -> SubViewport:
	var viewport := super._new_client_viewport()
	viewport.size = Vector2i(1920, 1080)
	return viewport
