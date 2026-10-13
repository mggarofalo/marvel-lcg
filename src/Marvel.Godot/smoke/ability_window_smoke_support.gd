extends RefCounted


static func surface(main: Control) -> Control:
	for node in main.find_children("AbilityWindow", "Control", true, false):
		if not node.is_queued_for_deletion() and node.is_visible_in_tree():
			return node as Control
	return null


static func commitment(main: Control) -> Button:
	var window := surface(main)
	if window == null: return null
	for pattern in ["AcknowledgeForcedAbility", "WindowChoice*", "WindowAction*", "WindowPass", "Submit"]:
		for candidate in window.find_children(pattern, "Button", true, false):
			var button := candidate as Button
			if button.is_visible_in_tree() and not button.disabled:
				return button
	return null
