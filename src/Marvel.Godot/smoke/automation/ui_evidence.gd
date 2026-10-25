extends RefCounted

# Evidence contains rendered control descriptions, never arbitrary object
# properties or engine state. Editable fields (including secrets) are omitted.
static func capture(root: Node, viewport: Viewport, message: String, steps: Array) -> String:
    var directory := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    if directory.is_empty():
        directory = OS.get_cache_dir().path_join("marvel-ui-evidence")
    directory = directory.path_join("%s-%s" % [OS.get_process_id(), Time.get_ticks_usec()])
    if DirAccess.make_dir_recursive_absolute(directory) != OK: return ""
    var controls: Array = []
    if is_instance_valid(root): _describe(root, controls)
    var screenshot := "unavailable: headless display"
    if DisplayServer.get_name() != "headless" and is_instance_valid(viewport):
        var texture := viewport.get_texture()
        if texture != null:
            var image := texture.get_image()
            if image != null and not image.is_empty():
                screenshot = "failure.png" if image.save_png(directory.path_join("failure.png")) == OK else "unavailable: PNG write failed"
    var file := FileAccess.open(directory.path_join("failure.json"), FileAccess.WRITE)
    if file == null: return ""
    file.store_string(JSON.stringify({"message": message, "steps": steps,
        "controls": controls, "screenshot": screenshot}, "  "))
    return directory

static func _describe(node: Node, controls: Array) -> void:
    if node is Control and node.is_visible_in_tree():
        var entry := {"path": str(node.get_path()), "type": node.get_class(),
            "bounds": str(node.get_global_rect()), "focused": node.has_focus()}
        if node is Button or node is Label: entry["text"] = node.text
        if node is RichTextLabel: entry["text"] = node.get_parsed_text()
        if node is BaseButton: entry["disabled"] = node.disabled
        controls.append(entry)
    for child in node.get_children(): _describe(child, controls)
