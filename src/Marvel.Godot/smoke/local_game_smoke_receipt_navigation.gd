extends RefCounted

const CaptionBounds = preload("res://smoke/local_game_smoke_caption_bounds.gd")

# Reads real legal-Core receipts; no text or scroll value is injected.
static func read_previous_receipt(driver) -> bool:
	var scroll := driver.main.find_child("LatestResultScroll", true, false) as ScrollContainer
	if scroll == null or not scroll.is_visible_in_tree():
		return true
	var more := scroll.get_parent().get_node_or_null("OverflowNavigation/More") as Button
	var bar := scroll.get_v_scroll_bar()
	bar.value_changed.connect(func(value):
		if value == 0 and is_instance_valid(driver.main):
			navigation_state(driver, "range reached zero"))
	print("RECEIPT_NAVIGATION_PROBE value=%s max=%s page=%s more=%s" % [
		scroll.scroll_vertical, bar.max_value, bar.page,
		more != null and more.is_visible_in_tree()])
	if more == null or not more.is_visible_in_tree():
		return true
	if not await driver._pointer_activate(more) or scroll.scroll_vertical <= 0:
		driver._fail("the visible result continuation did not expose later text")
		return false
	for problem in CaptionBounds.problems(driver.main):
		driver._fail(problem)
		return false
	var label := driver.main.find_child("LatestResult", true, false) as Label
	if label == null or scroll.size.y < label.get_line_height() * 3:
		driver._fail("result navigation leaves less than three lines of reading space")
		return false
	navigation_state(driver, "read previous")
	return true


static func replacement_receipt_starts_at_top(driver, action: String = "Web-Shooter") -> bool:
	navigation_state(driver, "replacement " + action)
	if not await driver._wait_for(func() -> bool:
		var scroll := driver.main.find_child("LatestResultScroll", true, false) as ScrollContainer
		return scroll != null and scroll.scroll_vertical == 0):
		driver._fail("the new %s receipt retained the previous result's scroll position" % action)
		return false
	return true


static func navigation_state(driver, stage: String) -> void:
	var scroll := driver.main.find_child("LatestResultScroll", true, false) as ScrollContainer
	var label := driver.main.find_child("LatestResult", true, false) as Label
	if scroll != null and label != null:
		var bar := scroll.get_v_scroll_bar()
		print("RECEIPT_STATE stage=%s value=%s max=%s page=%s length=%s" % [
			stage, scroll.scroll_vertical, bar.max_value, bar.page, label.text.length()])

