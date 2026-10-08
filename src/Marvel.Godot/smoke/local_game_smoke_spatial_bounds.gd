extends RefCounted

const CaptionBounds = preload("res://smoke/local_game_smoke_caption_bounds.gd")

# Geometry checks use transformed corners, including exhausted and fanned faces.
# The checks establish separation, not comprehension of the player's choices.
static func problems(main: Control) -> Array[String]:
	var failures: Array[String] = []
	var surface := main.find_child("AstraTableSurface", true, false) as Control
	if surface == null:
		return failures
	var context := main.find_child("ContextualDecision", true, false) as Control
	for node in surface.find_children("ProceduralCard*", "PanelContainer", true, false):
		var card := node as Control
		if not card.is_visible_in_tree() or context != null and context.is_ancestor_of(card):
			continue
		failures.append_array(card_problems(card, context))
	failures.append_array(hand_face_problems(surface))
	failures.append_array(hand_control_problems(surface))
	failures.append_array(navigation_problems(main))
	failures.append_array(drawer_problems(surface))
	failures.append_array(controlled_source_problems(surface, context))
	failures.append_array(CaptionBounds.problems(main))
	return failures


static func hand_face_problems(surface: Control) -> Array[String]:
	var failures: Array[String] = []
	var cards := surface.find_children("ProceduralCard*", "PanelContainer", true, false)
	for node in cards:
		var hand := node as Control
		if not hand.has_meta("spatial_hand_index") or not hand.is_visible_in_tree():
			continue
		for other in cards:
			if other.has_meta("spatial_hand_index") or not other.is_visible_in_tree():
				continue
			if bounds(hand).grow(-1).intersects(bounds(other).grow(-1)):
				failures.append("%s hand face overlaps installed %s" % [hand.name, other.name])
	return failures


static func card_problems(card: Control, context: Control) -> Array[String]:
	var failures: Array[String] = []
	var face := bounds(card)
	if context != null and face.grow(-1).intersects(context.get_global_rect().grow(-1)):
		failures.append("%s covers the current decision" % card.name)
	var sidecar := card.get_node_or_null("SpatialOverlay/SpatialControls") as Control
	if sidecar == null:
		return failures
	var occupied := bounds(sidecar)
	if occupied.position.x < face.end.x + 3.0 or occupied.grow(-1).intersects(face.grow(-1)):
		failures.append("%s source controls cover its face: %s / %s" % [card.name, occupied, face])
	if absf(sidecar.get_global_transform().get_rotation()) > 0.001:
		failures.append("%s source controls are not upright" % card.name)
	if context != null and occupied.grow(-1).intersects(context.get_global_rect().grow(-1)):
		failures.append("%s source controls cover the decision" % card.name)
	return failures


static func hand_control_problems(surface: Control) -> Array[String]:
	var failures: Array[String] = []
	var hand_controls: Array[Control] = []
	for node in surface.find_children("Card*", "Button", true, false):
		var control := node as Control
		if control.has_meta("spatial_hand_control") and control.is_visible_in_tree():
			hand_controls.append(control)
	for index in range(hand_controls.size()):
		var control := hand_controls[index]
		if control.text == "Play" and control.size.y > 60:
			failures.append("%s hand control extends beyond its reserved strip" % control.name)
		if absf(control.get_global_transform().get_rotation()) > 0.001:
			failures.append("%s hand control is not upright" % control.name)
		for other_index in range(index + 1, hand_controls.size()):
			var other := hand_controls[other_index]
			if bounds(control).grow(-1).intersects(bounds(other).grow(-1)):
				failures.append("%s and %s hand controls overlap" % [control.name, other.name])
	return failures


static func navigation_problems(main: Control) -> Array[String]:
	var failures: Array[String] = []
	for name in ["CausalContext", "ContextualActionScroll", "LatestResultScroll"]:
		var scroll := main.find_child(name, true, false) as ScrollContainer
		if scroll == null or not scroll.is_visible_in_tree():
			continue
		failures.append_array(continuation_problems(scroll, name))
	return failures


static func continuation_problems(scroll: ScrollContainer, name: String) -> Array[String]:
	var failures: Array[String] = []
	var bar := scroll.get_v_scroll_bar()
	if bar.max_value <= bar.page + 1.0:
		return failures
	var navigation := scroll.get_parent().get_node_or_null("OverflowNavigation") as Control
	if navigation == null or not navigation.is_visible_in_tree():
		failures.append("%s clips text without a continuation control" % name)
		return failures
	var saw_navigation := false
	for child in navigation.get_children():
		if child is Button and child.is_visible_in_tree():
			saw_navigation = true
			if not bounds(navigation).encloses(bounds(child)):
				failures.append("%s continuation control is clipped" % name)
	if not saw_navigation:
		failures.append("%s has no visible continuation control" % name)
	return failures


static func drawer_problems(surface: Control) -> Array[String]:
	var failures: Array[String] = []
	for node in surface.find_children("InspectRegion*", "MenuButton", true, false):
		var drawer := node as Control
		if not drawer.is_visible_in_tree():
			continue
		var parent := drawer.get_parent()
		if parent is VBoxContainer:
			var scroll := parent.get_parent() as Control
			if not bounds(scroll).encloses(bounds(drawer)):
				failures.append("%s region drawer is clipped by its source controls" % drawer.name)
		for other in surface.find_children("ProceduralCard*", "PanelContainer", true, false):
			if other.is_visible_in_tree() and bounds(drawer).grow(-1).intersects(bounds(other).grow(-1)):
				failures.append("%s region drawer covers %s" % [drawer.name, other.name])
	return failures


static func controlled_source_problems(surface: Control, context: Control) -> Array[String]:
	var failures: Array[String] = []
	var ledger := surface.get_node_or_null("ControlledSourceLedger") as Control
	if ledger == null or not ledger.is_visible_in_tree(): return failures
	var picker := ledger.get_node("SourcePicker") as Control
	var occupied := bounds(ledger)
	if not bounds(surface).encloses(occupied):
		failures.append("controlled source ledger escapes the table")
	if context != null and occupied.intersects(bounds(context)):
		failures.append("controlled source ledger covers the current decision")
	if picker.size.y < 44:
		failures.append("controlled source picker loses its full hit area")
	for other in surface.find_children("ProceduralCard*", "PanelContainer", true, false):
		if not other.is_visible_in_tree() or ledger.is_ancestor_of(other): continue
		# Six pixels include the outer focus stroke, even before this card gains focus.
		if occupied.intersects(bounds(other).grow(6)):
			failures.append("controlled source ledger covers %s face or focus frame: %s / %s" % [other.name, occupied, bounds(other)])
	return failures


static func bounds(control: Control) -> Rect2:
	var transform := control.get_global_transform()
	var size := control.size
	var corners := [transform * Vector2.ZERO, transform * Vector2(size.x, 0),
		transform * size, transform * Vector2(0, size.y)]
	var result := Rect2(corners[0], Vector2.ZERO)
	for corner in corners:
		result = result.expand(corner)
	return result
