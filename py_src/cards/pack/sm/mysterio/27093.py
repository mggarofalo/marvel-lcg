from . import *

# Fearmonger

def GetAbilities() -> Sequence['Ability']:

    def fearmonger_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardAllHands(effect)
        player.DrawUp(player.GetIdentity().hand_size, effect)


    def fearmonger_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("BB"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Deal this card to yourself as a facedown encounter card",
                lambda targets:
                    player.DealEncounterCard(this, effect)
            )
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            fearmonger_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            fearmonger_boost
        ),
    ]

