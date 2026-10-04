extends "res://smoke/hosted_multiplayer_smoke_client_support.gd"

# Synthetic observer cases, separate from the legal two-client game journey.
# Each response is already settled before the observer starts.
var failure_message := ""
var failed_case := ""


func verify() -> bool:
	var cases := [
		{"name": "fast accepted response", "revision": 7, "status": "READY FOR YOUR CHOICE", "accepted": true, "error": ""},
		{"name": "no accepted response", "revision": 6, "status": "READY FOR YOUR CHOICE", "accepted": false, "error": "did not advance"},
		{"name": "rejected response", "revision": 6, "status": "DECISION REJECTED", "accepted": false, "error": "was not accepted"},
		{"name": "duplicate accepted response", "revision": 8, "status": "READY FOR YOUR CHOICE", "accepted": false, "error": "exactly one revision"},
	]
	for example in cases:
		failure_message = ""
		var main := _response_surface(example.revision, example.status)
		add_child(main)
		var accepted := await _wait_for_hosted_settlement(main, 6)
		main.queue_free()
		var expected_error: String = example.error
		if accepted != example.accepted \
				or (expected_error.is_empty() and not failure_message.is_empty()) \
				or (not expected_error.is_empty() and expected_error not in failure_message):
			failed_case = "%s: accepted=%s error=%s" % [example.name, accepted, failure_message]
			return false
	return true


func _response_surface(revision: int, status: String) -> Control:
	var main := Control.new()
	var status_bar := Node.new()
	status_bar.name = "StatusBar"
	main.add_child(status_bar)
	var sync := Label.new()
	sync.name = "SyncStatus"
	sync.text = "Last synced · r%d" % revision
	status_bar.add_child(sync)
	var parent: Node = main
	for name in ["Margin", "Shell", "Content", "Status"]:
		var child := Node.new()
		child.name = name
		parent.add_child(child)
		parent = child
	var text := Label.new()
	text.name = "Text"
	text.text = status
	parent.add_child(text)
	return main


func _wait_for(condition: Callable) -> bool:
	return condition.call()


func _fail(message: String) -> void:
	failure_message = message
