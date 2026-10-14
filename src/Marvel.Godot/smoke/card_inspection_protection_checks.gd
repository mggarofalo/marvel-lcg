extends RefCounted

static func verify(driver, frame: Control) -> bool:
	var bounds := frame.get_global_rect()
	var regions: Array = frame.get_meta("protected_regions", [])
	if regions.is_empty():
		driver._fail("inspection has no protected live context")
		return false
	for region: Rect2 in regions:
		if bounds.intersects(region):
			driver._fail("inspection obscures protected context: frame=%s protected=%s" % [bounds, region])
			return false
	print("INSPECTION_PROTECTION frame=%s protected=%s bounded=%s" % [bounds, regions, frame.get_meta("bounded_inspection", false)])
	return true
