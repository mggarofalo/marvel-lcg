extends RefCounted

# A separated table shows every heading, marker and progress line whole, and no placed card,
# encounter stack or attachment strip covers a heading, a marker or a column's explanation.
const MEASURED := ["GameArea*Heading", "GameArea*Markers", "KangProgress", "SharedEncounterNote"]
const COVERED := ["GameArea*Heading", "GameArea*Markers", "GameArea*DetailsViewport"]

static func check(smoke, surface: Control, leaf: String) -> bool:
    if surface.find_child("GameAreaCentral", false, false) == null: return true
    for label in _named(surface, MEASURED):
        if label.size.y + 0.5 < label.get_combined_minimum_size().y:
            smoke._fail("%s: %s shows %.0f of its %.0f pixels" % [leaf, label.name, label.size.y,
                label.get_combined_minimum_size().y])
            return false
    var texts := _named(surface, COVERED)
    for placed in _placed(surface):
        for text in texts:
            if placed[1].intersects(_rect(text)):
                smoke._fail("%s: %s %s covers %s %s" % [leaf, placed[0], placed[1], text.name, _rect(text)])
                return false
    return true

static func _named(surface: Control, patterns: Array) -> Array:
    var found := []
    for pattern in patterns:
        found.append_array(surface.find_children(pattern, "Control", true, false).filter(
            func(node): return node.is_visible_in_tree() and node.size.y > 0))
    return found

# Each card's face, the attachment strip hung below it, and each encounter stack's column slot.
static func _placed(surface: Control) -> Array:
    var placed := []
    for node in surface.find_children("*", "Control", true, false):
        if node.get_meta("game_area_card", false):
            placed.append([node.name, _rect(node)])
            var strip = node.get_node_or_null("SpatialOverlay/SpatialControls")
            if strip != null and strip.is_visible_in_tree() and strip.get_node("Contents").get_child_count() > 0:
                placed.append([str(node.name) + " attachments", _rect(strip)])
        elif node.get_meta("game_area_stack", false):
            placed.append([node.name, _rect(node)])
    return placed

static func _rect(node: Control) -> Rect2:
    var transform := node.get_global_transform()
    return Rect2(transform.origin, node.size * transform.get_scale())
