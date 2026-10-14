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
	return card.get_global_transform().is_equal_approx(expected)

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
