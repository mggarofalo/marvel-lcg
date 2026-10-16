extends RefCounted

static func verify(smoke) -> bool:
    var sources := _cards(smoke, "source_title")
    var hand := _cards(smoke, "spatial_hand_index")
    if sources.size() != 5 or hand.size() != 5:
        smoke._fail("live dense Core scene must retain five installed sources and five hand cards")
        return false
    for source in sources:
        if not await smoke._prepare_activation(source): return false
        if not smoke._control_is_fully_visible(source):
            smoke._fail("live scaling clipped an installed source")
            return false
        if not _meaning_is_readable(smoke, source):
            var meaning := source.find_child("SourceMeaning", true, false) as RichTextLabel
            smoke._fail("live source meaning unavailable: %s scale=%d visible=%s height=%s" % [source.get_meta("source_title"),
                int(smoke._node("Toolbar/InterfaceScale").value), meaning != null and meaning.is_visible_in_tree(),
                "missing" if meaning == null else "%s/%s" % [meaning.get_content_height(), meaning.size.y]])
            return false
        for card in hand:
            if source.get_global_rect().intersects(card.get_global_rect()):
                smoke._fail("live scaling moved installed sources over the hand")
                return false
    return true

static func _cards(smoke, metadata: String) -> Array[Control]:
    var cards: Array[Control] = []
    for card in smoke.main.find_children("ProceduralCard*", "Control", true, false):
        if card.is_visible_in_tree() and card.has_meta(metadata): cards.append(card)
    return cards

static func _meaning_is_readable(smoke, source: Control) -> bool:
    var meaning := source.find_child("SourceMeaning", true, false) as RichTextLabel
    if meaning == null:
        var contribution := source.find_child("SourceContributions", true, false) as Control
        return contribution != null and contribution.is_visible_in_tree() and smoke._control_is_fully_visible(contribution)
    if not meaning.is_visible_in_tree() or meaning.visible_characters == 0: return false
    if meaning.get_content_height() <= meaning.size.y + 1.0: return true
    var disclosure := meaning.find_child("SourceRulesDisclosure", true, false) as Label
    return meaning.visible_characters > 0 and meaning.get_line_height(0) <= meaning.size.y \
        and disclosure != null and disclosure.is_visible_in_tree() and smoke._control_is_fully_visible(disclosure)
