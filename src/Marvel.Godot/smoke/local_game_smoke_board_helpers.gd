extends RefCounted


static func tabletop_card_named(main: Control, title: String) -> Control:
	for candidate in main.find_children("ProceduralCard", "PanelContainer", true, false):
		var card := candidate as Control
		var card_title := card.find_child("Title", true, false) as Label
		if card_title != null and card_title.text == title:
			return card
	return null


static func focused_cards(main: Control) -> Array[Control]:
	var focused: Array[Control] = []
	for candidate in main.find_children("ProceduralCard", "", true, false):
		var card := candidate as Control
		if card != null and card.theme_type_variation == &"FocusedCard":
			focused.append(card)
	return focused
