extends SceneTree

const BOARD_HELPERS := preload("res://smoke/local_game_smoke_board_helpers.gd")


func _control_is_fully_visible(control: Control) -> bool:
	return BOARD_HELPERS.fully_contained(self, control)
