extends RefCounted

static func verify(driver, source: Control) -> bool:
	if not await driver._pointer_activate(source): return false
	var inspector: Control = driver.main.get_node("CardInspector")
	if not inspector.visible or inspector.get_node("Frame/Stack/Header/Close").visible:
		driver._fail("pointer click must preserve transient preview: " + str(source.name))
		return false
	if not source.tooltip_text.is_empty():
		driver._fail("preview source repeats inspection instructions in a tooltip")
		return false
	return true
