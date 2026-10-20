extends "res://smoke/hosted_multiplayer_smoke_support.gd"

# Synthetic native hit coverage: moving ancestors do not change control identity.
var failure_message := ""
var failed_case := ""
var samples: Array[Vector2] = []
var mode := ""


func verify() -> bool:
	for example in ["moving", "center", "top_left", "top_right", "bottom_left", "bottom_right", "clipped", "replaced"]:
		mode = example
		failure_message = ""
		samples.clear()
		var viewport := _new_client_viewport()
		var parent := Control.new()
		parent.position = Vector2(80, 80)
		parent.size = Vector2(180, 80)
		viewport.add_child(parent)
		var button := Button.new()
		button.name = "HitSubject"
		button.position = Vector2(10, 10)
		button.size = Vector2(150, 44)
		parent.add_child(button)
		var obstruction := ["center", "top_left", "top_right", "bottom_left", "bottom_right"].find(example)
		if obstruction >= 0:
			var points := [Vector2(75, 22), Vector2(2, 2), Vector2(148, 2), Vector2(2, 42), Vector2(148, 42)]
			var cover := Control.new()
			cover.position = button.position + points[obstruction] - Vector2(1, 1)
			cover.size = Vector2(3, 3)
			cover.mouse_filter = Control.MOUSE_FILTER_STOP
			parent.add_child(cover)
		if example == "clipped":
			parent.clip_contents = true
			parent.size.x = 13
		await get_tree().process_frame
		var accepted := await _control_has_real_hit_area(button)
		var valid: bool = accepted == (example == "moving")
		if example == "moving":
			valid = valid and samples.size() == 5 and failure_message.is_empty()
			if valid:
				# Current rectangle moves 2.5 pixels between each sample.
				var expected: Array[Vector2] = [Vector2(165, 112), Vector2(92, 94.5),
					Vector2(238, 97), Vector2(92, 139.5), Vector2(238, 142)]
				valid = samples == expected
		elif obstruction >= 0:
			valid = valid and samples.size() == obstruction + 1 and "loses hit" in failure_message
		elif example == "clipped":
			valid = valid and samples.is_empty() and "unclipped hit area" in failure_message
		else:
			valid = valid and samples.size() == 1 and "replaced" in failure_message
		viewport.queue_free()
		await get_tree().process_frame
		if not valid:
			failed_case = "%s: accepted=%s samples=%s error=%s" % [example, accepted, samples, failure_message]
			return false
	return true


func _control_owns_point(control: Control, point: Vector2) -> bool:
	samples.append(point)
	var owned := super._control_owns_point(control, point)
	if mode == "moving":
		_shift.call_deferred(control.get_parent())
	elif mode == "replaced":
		_replace.call_deferred(control)
	return owned


func _shift(parent: Control) -> void:
	if is_instance_valid(parent): parent.position.y += 2.5


func _replace(control: Control) -> void:
	var replacement := Button.new()
	replacement.name = control.name
	replacement.position = control.position
	replacement.size = control.size
	var parent := control.get_parent()
	parent.remove_child(control)
	control.queue_free()
	parent.add_child(replacement)


func _fail(message: String) -> void:
	failure_message = message
