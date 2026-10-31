extends RefCounted

# Observe the required final pose without assuming a particular animation duration.
static func wait_for(tree: SceneTree, condition: Callable) -> bool:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < 750:
		if condition.call():
			# Native global transforms must reflect the restored local pose before a baseline is read.
			await tree.process_frame
			if condition.call(): return true
		else:
			await tree.process_frame
	return false

# Container layout can converge over several frames after a decision changes the
# surrounding chrome, moving a card whose local pose is already authored. A gesture
# baseline needs the card's on-screen pose unchanged across consecutive frames.
static func stable_global_pose(tree: SceneTree, card: Control, frames := 3, deadline_ms := 3000) -> bool:
	var started := Time.get_ticks_msec()
	var previous := Transform2D()
	var unchanged := -1
	while Time.get_ticks_msec() - started < deadline_ms:
		if not is_instance_valid(card) or not card.is_inside_tree(): return false
		card.force_update_transform()
		var current := card.get_global_transform()
		unchanged = unchanged + 1 if unchanged >= 0 and current.is_equal_approx(previous) else 0
		if unchanged >= frames: return true
		previous = current
		await tree.process_frame
	return false

static func authored_pose(tree: SceneTree, card: Control) -> bool:
	var position := card.position
	var rotation := card.rotation
	return await wait_for(tree, func() -> bool:
		return is_instance_valid(card) and card.is_inside_tree() and _local_resting_pose(card, position, rotation))

static func _local_resting_pose(card: Control, position: Vector2, rotation: float) -> bool:
	return _return_finished(card) \
		and card.position.is_equal_approx(card.get_meta("spatial_resting_position", position)) \
		and is_equal_approx(card.rotation, card.get_meta("spatial_resting_rotation", rotation))

static func _return_finished(card: Control) -> bool:
	return not bool(card.get_meta("card_return_active", false)) \
		and not card.is_set_as_top_level() and card.modulate == Color.WHITE

static func _global_matches(card: Control, expected: Transform2D) -> bool:
	card.force_update_transform()
	var actual := card.get_global_transform()
	# Composed parent transforms may settle by a fraction of a pixel. Keep basis
	# checks strict and bound positional error below a tenth of a rendered pixel.
	return actual.x.is_equal_approx(expected.x) and actual.y.is_equal_approx(expected.y) \
		and actual.origin.distance_to(expected.origin) < 0.1

static func _authored_pose_matches(card: Control) -> bool:
	if not card.has_meta("spatial_resting_position"): return true
	return _local_resting_pose(card, card.position, card.rotation)

static func pose(tree: SceneTree, card: Control, position: Vector2, rotation: float) -> bool:
	return await wait_for(tree, func() -> bool:
		return is_instance_valid(card) and card.is_inside_tree() \
			and _return_finished(card) and card.position == position \
			and card.rotation == rotation and card.modulate == Color.WHITE)

static func global_pose(tree: SceneTree, card: Control, expected: Transform2D) -> bool:
	return await wait_for(tree, func() -> bool:
		return is_instance_valid(card) and card.is_inside_tree() \
			and _return_finished(card) and _authored_pose_matches(card) \
			and _global_matches(card, expected) \
			and card.modulate == Color.WHITE)

static func returned_or_retired(tree: SceneTree, card, expected: Transform2D) -> bool:
	if not is_instance_valid(card): return true
	var tracked: WeakRef = weakref(card)
	return await wait_for(tree, func() -> bool:
		var current := tracked.get_ref() as Control
		if current == null or not current.is_inside_tree(): return true
		return _return_finished(current) and _authored_pose_matches(current) \
			and _global_matches(current, expected) \
			and current.modulate == Color.WHITE)

static func hand(driver) -> void:
	var cards: Array[Node] = driver._hand_surface().find_children("ProceduralCard*", "", true, false)
	if not await wait_for(driver, func() -> bool: return _hand_is_resting(cards)):
		driver._fail("hand did not finish returning to its authored poses within 750 ms")

static func _hand_is_resting(cards: Array[Node]) -> bool:
	for candidate in cards:
		if not candidate.has_meta("spatial_hand_index"): continue
		var card := candidate as Control
		if not _local_resting_pose(card, card.position, card.rotation): return false
	return true


# Synthetic native geometry: a fractional parent correction is visually bounded,
# while an altered authored local pose or rotated basis remains a failed return.
static func fractional_parent_probe(tree: SceneTree) -> bool:
	var parent := Control.new()
	var card := Control.new()
	tree.root.add_child(parent)
	parent.add_child(card)
	parent.position = Vector2(0.25, 0.25)
	parent.scale = Vector2(0.8, 0.8)
	card.rotation = PI / 2.0
	card.set_meta("spatial_resting_position", card.position)
	card.set_meta("spatial_resting_rotation", card.rotation)
	card.force_update_transform()
	var expected := card.get_global_transform()
	parent.position += Vector2(0.03125, 0.0)
	card.force_update_transform()
	var distinguishes_coordinate_tolerance := not card.get_global_transform().is_equal_approx(expected)
	var bounded_parent := await global_pose(tree, card, expected)
	card.position = Vector2(0.25, 0.0)
	var rejects_local := not _authored_pose_matches(card)
	card.position = Vector2.ZERO
	card.rotation += 0.01
	var rejects_rotation := not _global_matches(card, expected)
	card.rotation = PI / 2.0
	parent.position += Vector2(0.125, 0.0)
	var rejects_displacement := not _global_matches(card, expected)
	parent.queue_free()
	return distinguishes_coordinate_tolerance and bounded_parent and rejects_local \
			and rejects_rotation and rejects_displacement
