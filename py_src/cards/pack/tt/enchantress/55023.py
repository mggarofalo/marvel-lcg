from . import *

# "Do My Bidding"

def GetAbilities() -> Sequence['Ability']:

    def do_my_bidding_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not identity.HasTrait("DEFIANT") and not identity.HasTrait("ENTHRALLED"):
            return

        def action(card_type: Type[CardFace]):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                card_type=card_type
            )
            if face:
                face.Reveal(player, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Search the encounter deck and discard pile for a minion and reveal it.",
                lambda targets:
                    action(Minion)
            ),
            AbilityFactory.ForChoiceAbility(
                "Search the encounter deck and discard pile for a side scheme and reveal it.",
                lambda targets:
                    action(SchemeSide2)
            ),
            choose_x_different_options=2 if identity.HasTrait("ENTHRALLED") else 1
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            do_my_bidding_revealed
        ),
    ]

