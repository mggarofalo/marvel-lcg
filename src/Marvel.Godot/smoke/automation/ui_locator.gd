extends RefCounted

# Test policy: locators are lazy, visible-only and strict. Scope duplicates
# explicitly; selecting the first match would hide an ambiguous interface.
var scope: Node
var description: String
var predicate: Callable

func _init(root: Node, label: String, matches: Callable) -> void:
    scope = root
    description = label
    predicate = matches

func all() -> Array[Control]:
    var found: Array[Control] = []
    if is_instance_valid(scope): _collect(scope, found)
    return found

func _collect(node: Node, found: Array[Control]) -> void:
    if node.is_queued_for_deletion(): return
    if node is Control and node.is_visible_in_tree() and predicate.call(node):
        found.append(node)
    for child in node.get_children(): _collect(child, found)

static func button(root: Node, text: String, exact: bool = true) -> RefCounted:
    return new(root, "button '%s'" % text, func(node: Control) -> bool:
        return node is Button and (node.text == text if exact else text.to_lower() in node.text.to_lower()))

static func named(root: Node, node_name: String) -> RefCounted:
    return new(root, "control '%s'" % node_name, func(node: Control) -> bool:
        return str(node.name) == node_name)

static func text(root: Node, wanted: String) -> RefCounted:
    return new(root, "text '%s'" % wanted, func(node: Control) -> bool:
        return (node is Label and node.text == wanted) or \
            (node is RichTextLabel and node.get_parsed_text() == wanted))

static func control(target: Control) -> RefCounted:
    var label := str(target.get_path()) if is_instance_valid(target) else "freed control"
    return new(target, label, func(node: Control) -> bool: return node == target)
