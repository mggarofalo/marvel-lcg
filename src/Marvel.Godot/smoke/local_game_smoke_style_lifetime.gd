extends RefCounted

# Retire the owner through the normal frame lifecycle, then check its resources.
static func release(main: Control) -> Array[String]:
	var tree := main.get_tree()
	var observed := observe(main)
	var themes := observe_themes(main)
	var failures: Array[String] = []
	if observed.is_empty() and OS.get_environment("MARVEL_SMOKE_VIEWPORT") == "1920x1080":
		failures.append("the desktop journey exercised no compact pile styles")
	if themes.size() != 2:
		failures.append("the desktop shell has no installed main and status themes")
	main.queue_free()
	main = null
	await tree.process_frame
	await tree.process_frame
	failures.append_array(problems(observed))
	failures.append_array(problems(themes, "fresh theme"))
	return failures


# Observe only weak references: the control must own its independent style copies.
static func observe(main: Control) -> Array[WeakRef]:
	var observed: Array[WeakRef] = []
	for button in main.find_children("InspectPile*", "Button", true, false):
		for state in ["normal", "hover", "pressed", "hover_pressed", "disabled"]:
			var style: StyleBox = button.get_theme_stylebox(state)
			observed.append(weakref(style))
	return observed


static func observe_themes(main: Control) -> Array[WeakRef]:
	var observed: Array[WeakRef] = []
	for control in [main, main.get_node("StatusBar")]:
		if control.theme != null:
			observed.append(weakref(control.theme))
	return observed


static func problems(observed: Array[WeakRef], kind: String = "compact button style") -> Array[String]:
	var failures: Array[String] = []
	for resource in observed:
		if resource.get_ref() != null:
			failures.append("a %s outlived its freed control" % kind)
	return failures
