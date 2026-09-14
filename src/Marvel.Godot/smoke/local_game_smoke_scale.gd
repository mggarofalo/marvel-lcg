extends RefCounted

static func percentage(configured: String) -> int:
	match configured.strip_edges().to_lower():
		"compact", "":
			return 80
		"standard":
			return 100
		"large":
			return 120
		"extra-large":
			return 150
	return int(configured.trim_suffix("%"))


static func metric(base: int, configured: String) -> int:
	return ceili(base * percentage(configured) / 100.0)
