extends RefCounted

# Retire the owner through the normal frame lifecycle, then check its resources.
static func release(main: Control) -> Array[String]:
	var tree := main.get_tree()
	var observed := observe(main)
	var failures: Array[String] = []
	if observed.is_empty() and OS.get_environment("MARVEL_SMOKE_VIEWPORT") == "1920x1080":
		failures.append("the desktop journey exercised no compact pile styles")
	main.queue_free()
	main = null
	await tree.process_frame
	await tree.process_frame
	failures.append_array(problems(observed))
	return failures


# Observe only weak references: the control must own its independent style copies.
static func observe(main: Control) -> Array[WeakRef]:
	var observed: Array[WeakRef] = []
	for button in main.find_children("InspectPile*", "Button", true, false):
		for state in ["normal", "hover", "pressed", "hover_pressed", "disabled"]:
			var style: StyleBox = button.get_theme_stylebox(state)
			observed.append(weakref(style))
	return observed


static func problems(observed: Array[WeakRef]) -> Array[String]:
	var failures: Array[String] = []
	for resource in observed:
		if resource.get_ref() != null:
			failures.append("a compact button style outlived its freed control")
	return failures
