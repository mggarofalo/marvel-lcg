extends RefCounted

static func problems(main: Control) -> Array[String]:
	var failures: Array[String] = []
	var viewport := main.find_child("ContextualActionScroll", true, false) as Control
	var rows := main.find_child("ContextualActionObjects", true, false) as Control
	if viewport == null or rows == null: return failures
	var frame := viewport.get_parent() as Control
	var complete := frame.get_node_or_null("ActionPageCompleteChoices") as Button
	if complete != null and complete.is_visible_in_tree():
		if not frame.get_global_rect().grow(0.5).encloses(complete.get_global_rect()) or complete.size.y < 44:
			failures.append("complete-choice continuation has no whole hit area")
		return failures
	if not rows.is_visible_in_tree(): return failures
	var navigation := frame.get_node_or_null("OverflowNavigation") as Control
	for row in rows.get_children():
		if not row is Control or not row.is_visible_in_tree(): continue
		if not viewport.get_global_rect().grow(0.5).encloses(row.get_global_rect()):
			failures.append("%s action row is only partly visible in its page" % row.name)
		if navigation != null and navigation.is_visible_in_tree() and row.get_global_rect().intersects(navigation.get_global_rect()):
			failures.append("%s action row overlaps page navigation" % row.name)
	if navigation != null and navigation.is_visible_in_tree() and not frame.get_global_rect().grow(0.5).encloses(navigation.get_global_rect()):
		failures.append("action page navigation exceeds its frame")
	return failures


static func traverse(driver) -> bool:
	var rows := driver.main.find_child("ContextualActionObjects", true, false) as Control
	if rows == null or not rows.is_visible_in_tree(): return true
	var frame := rows.get_parent().get_parent() as Control
	var complete := frame.get_node_or_null("ActionPageCompleteChoices") as Button
	if complete != null and complete.is_visible_in_tree(): return true
	var expected: Array[int] = []
	for row in rows.get_children(): expected.append(row.get_instance_id())
	var seen: Array[int] = []
	if not await _traverse_forward(driver, rows, frame, expected.size(), seen): return false
	if seen != expected:
		driver._fail("action pages do not expose every offered row exactly once")
		return false
	return await _return_to_first_page(driver, frame, expected.size())


static func _traverse_forward(driver, rows: Control, frame: Control, count: int, seen: Array[int]) -> bool:
	for _page in range(count + 1):
		for problem in problems(driver.main):
			driver._fail(problem)
			return false
		if not _collect_visible_rows(driver, rows, seen): return false
		var more := frame.get_node_or_null("OverflowNavigation/More") as Button
		if more == null or not more.is_visible_in_tree(): break
		if not await driver._pointer_activate(more): return false
		await driver.process_frame
		await driver.process_frame
		if not _current_page_has_focus(driver, rows): return false
	return true


static func _collect_visible_rows(driver, rows: Control, seen: Array[int]) -> bool:
	for row in rows.get_children():
		if not row.is_visible_in_tree(): continue
		if seen.has(row.get_instance_id()):
			driver._fail("action page traversal repeats a row")
			return false
		seen.append(row.get_instance_id())
	return true


static func _current_page_has_focus(driver, rows: Control) -> bool:
	var focus: Control = driver.render_viewport.gui_get_focus_owner()
	if focus == null or not rows.is_ancestor_of(focus) or not focus.is_visible_in_tree():
		driver._fail("action page navigation did not focus a current-page choice")
		return false
	return true


static func _return_to_first_page(driver, frame: Control, count: int) -> bool:
	for _page in range(count + 1):
		var earlier := frame.get_node_or_null("OverflowNavigation/Earlier") as Button
		if earlier == null or not earlier.is_visible_in_tree(): break
		if not await driver._pointer_activate(earlier): return false
		await driver.process_frame
		await driver.process_frame
	return true


static func complete_entry(main: Control) -> Button:
	var entry := main.find_child("ActionPageCompleteChoices", true, false) as Button
	return entry if entry != null and entry.is_visible_in_tree() else null


static func open_complete_choices(driver, entry: Button) -> bool:
	var revision: String = driver._node("Toolbar/SyncStatus").text
	var decision: Control = driver._decision()
	var heading: Label = driver._node("Play/Prompt/Margin/Stack/PromptHeader/Heading")
	var prompt_title := heading.text
	if not await driver._pointer_activate(entry): return false
	if not await driver._wait_for(func() -> bool:
		var sheet := driver.main.find_child("CompleteChoicesFrame", true, false) as Control
		return sheet != null and sheet.is_visible_in_tree()):
		driver._fail("the oversized-choice entry did not open complete choices")
		return false
	var frame := driver.main.find_child("CompleteChoicesFrame", true, false) as Control
	if not driver._control_is_fully_visible(frame):
		driver._fail("the oversized-choice continuation exceeds the viewport")
		return false
	if driver._node("Toolbar/SyncStatus").text != revision or driver._decision() != decision or heading.text != prompt_title:
		driver._fail("opening complete choices replaced the decision surface or committed a response")
		return false
	print("COMPLETE_CHOICE_ROUTE_OK " + revision)
	return true
