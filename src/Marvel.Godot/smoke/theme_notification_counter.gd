extends Control

var theme_changes := 0

func _notification(what: int) -> void:
	if what == NOTIFICATION_THEME_CHANGED:
		theme_changes += 1
