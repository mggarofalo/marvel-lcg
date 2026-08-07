from . import *

# Multiple Man

def GetAbilities() -> Sequence['Ability']:

    def multiple_man(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.SearchForCard(
            effect,
            initiator,
            include_player_deck=True,
            include_player_hand_cards=True,
            name="Multiple Man",
            card_type=Ally
        )
        if face:
            face.PutIntoPlay(initiator, effect)

    # We add this condition to automatically stop the effect
    def has_multiple_man(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> bool:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        finder = CardFinder(
            name="Multiple Man",
        )

        return initiator.player_deck.FindCard(finder) != None or \
            initiator.hand_cards.FindCard(finder) != None

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            multiple_man,
            conditions=[has_multiple_man],
        )
    ]

