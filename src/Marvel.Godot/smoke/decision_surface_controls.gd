extends RefCounted

const SURFACES := ["LocalDecisionWorkspace", "PaymentWorkspace", "CompleteChoicesFrame"]

static func commit(main: Node) -> Button:
	for name in SURFACES:
		var surface := main.find_child(name, true, false)
		if _visible(surface):
			return _button(surface, "Submit")
	return _button(main, "ContextualCommit")

static func clear_draft(main: Node) -> Button:
	var local := main.find_child("LocalDecisionWorkspace", true, false)
	if _visible(local): return _button(local, "LocalChooseAction")
	var payment := main.find_child("PaymentWorkspace", true, false)
	if _visible(payment): return _button(payment, "CancelCardPlay")
	return _button(main, "ContextualCancelDraft")

static func payment(main: Node) -> Control:
	var local := main.find_child("LocalDecisionWorkspace", true, false)
	if _visible(local) and local.find_child("PaymentProgress", true, false) != null:
		return local as Control
	var workspace := main.find_child("PaymentWorkspace", true, false)
	return workspace as Control if _visible(workspace) else null

static func contains(main: Node, control: Control) -> bool:
	for name in SURFACES:
		var surface := main.find_child(name, true, false)
		if _visible(surface) and surface.is_ancestor_of(control): return true
	return false

static func _button(parent: Node, name: String) -> Button:
	var button := parent.find_child(name, true, false) as Button
	return button if _visible(button) else null

static func _visible(node: Node) -> bool:
	return is_instance_valid(node) and not node.is_queued_for_deletion() \
		and node is Control and node.is_visible_in_tree()
