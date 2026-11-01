extends RefCounted

# A separated table shows its tracker and every column header whole, on one line. No placed card,
# encounter stack, attachment strip or shared pile covers a header or the tracker, columns do not
# overlap, and no paragraph sits on the table: every label in a header or the tracker is one line.
const MEASURED := ["GameArea*Heading", "KangTracker"]
const COVERED := ["GameArea*Heading", "KangTracker", "SeparatedTableRules"]

static func check(smoke, surface: Control, leaf: String) -> bool:
    if surface.find_child("KangTracker", false, false) == null:
        smoke._fail("%s: the Kang table has no tracker" % leaf)
        return false
    if surface.find_child("GameAreaCentral", false, false) == null: return true
    for label in _named(surface, MEASURED):
        if label.size.y + 0.5 < label.get_combined_minimum_size().y:
            smoke._fail("%s: %s shows %.0f of its %.0f pixels" % [leaf, label.name, label.size.y,
                label.get_combined_minimum_size().y])
            return false
    if not _one_line(smoke, surface, leaf): return false
    var texts := _named(surface, COVERED)
    for placed in _placed(surface):
        for text in texts:
            if placed[1].intersects(_rect(text)):
                smoke._fail("%s: %s %s covers %s %s" % [leaf, placed[0], placed[1], text.name, _rect(text)])
                return false
    return _columns_apart(smoke, surface, leaf)

static func _one_line(smoke, surface: Control, leaf: String) -> bool:
    if not surface.find_children("GameArea*Details*", "Control", true, false).is_empty():
        smoke._fail("%s: a column still carries an explanation paragraph" % leaf)
        return false
    for owner in _named(surface, MEASURED):
        for label in owner.find_children("*", "Label", true, false):
            if label.is_visible_in_tree() and label.get_line_count() > 1:
                smoke._fail("%s: %s/%s wraps to %d lines" % [leaf, owner.name, label.name, label.get_line_count()])
                return false
    return true

static func _columns_apart(smoke, surface: Control, leaf: String) -> bool:
    var frames := surface.find_children("GameArea*", "Panel", false, false).filter(
        func(node): return node.is_visible_in_tree() and node.has_meta("table_facts"))
    for first in range(frames.size()):
        for second in range(first + 1, frames.size()):
            if _rect(frames[first]).grow(-1).intersects(_rect(frames[second]).grow(-1)):
                smoke._fail("%s: columns %s and %s overlap" % [leaf, frames[first].name, frames[second].name])
                return false
    return true

static func _named(surface: Control, patterns: Array) -> Array:
    var found := []
    for pattern in patterns:
        found.append_array(surface.find_children(pattern, "Control", true, false).filter(
            func(node): return node.is_visible_in_tree() and node.size.y > 0))
    return found

# Each card's face, the attachment strip hung below it, each encounter stack's column slot and each pile.
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
        elif str(node.name).begins_with("Pile") and node is PanelContainer and node.is_visible_in_tree():
            placed.append([node.name, _rect(node)])
    return placed

static func _rect(node: Control) -> Rect2:
    var transform := node.get_global_transform()
    return Rect2(transform.origin, node.size * transform.get_scale())
