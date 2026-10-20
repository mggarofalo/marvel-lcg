extends RefCounted

# Player-card gestures restore their pose relative to independently laid-out table regions.
# Hand and player-tableau regions move by different amounts when chrome changes height.
static func capture(driver, card: Control, region_name: String = "") -> Dictionary:
	if region_name.is_empty(): region_name = "HandDropArea" if card.has_meta("spatial_hand_index") else "PlayerTable"
	var region: Control = driver.main.find_child(region_name, true, false)
	if region == null: region = card.get_parent() as Control
	return {"pose": card.get_global_transform(), "region": weakref(region), "origin": region.global_position}


static func expected(snapshot: Dictionary) -> Transform2D:
	var result: Transform2D = snapshot.pose
	var region: Control = snapshot.region.get_ref()
	if region != null: result.origin += region.global_position - snapshot.origin
	return result


static func settled(driver, card, snapshot: Dictionary, may_retire: bool = false) -> bool:
	if not is_instance_valid(card): return may_retire
	var tracked: WeakRef = weakref(card)
	return await preload("res://smoke/gesture_settlement.gd").wait_for(driver, func() -> bool:
		var current := tracked.get_ref() as Control
		if current == null or not current.is_inside_tree(): return may_retire
		return matches(current, snapshot))


static func matches(card: Control, snapshot: Dictionary) -> bool:
	if snapshot.region.get_ref() == null: return false
	var settlement = preload("res://smoke/gesture_settlement.gd")
	return settlement._return_finished(card) and settlement._authored_pose_matches(card) \
			and settlement._global_matches(card, expected(snapshot))


static func strict_probe(driver, card: Control) -> bool:
	var snapshot := capture(driver, card)
	var position := card.position
	var rotation := card.rotation
	var resting: Vector2 = card.get_meta("spatial_resting_position", position)
	var resting_rotation: float = card.get_meta("spatial_resting_rotation", rotation)
	var accepted := matches(card, snapshot)
	card.position += Vector2(2, 0)
	card.set_meta("spatial_resting_position", card.position)
	var rejects_shift := not matches(card, snapshot)
	card.position = position
	card.set_meta("spatial_resting_position", resting)
	card.rotation += 0.01
	card.set_meta("spatial_resting_rotation", card.rotation)
	var rejects_rotation := not matches(card, snapshot)
	card.rotation = rotation
	card.set_meta("spatial_resting_rotation", resting_rotation)
	return accepted and rejects_shift and rejects_rotation


static func reflow_round_trip(driver, card: Control, snapshot: Dictionary) -> bool:
	var origin: Vector2 = driver.main.position
	for position in [origin + Vector2(0, 24), origin]:
		driver.main.position = position
		for frame in 5: await driver.process_frame
		if not await settled(driver, card, snapshot): return false
	return true
