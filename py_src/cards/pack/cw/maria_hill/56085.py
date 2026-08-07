from . import *

# Executive Order

def GetAbilities() -> Sequence['Ability']:

    def executive_order_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", 2, effect)
        if Worlds.FindCardOnField(
            effect,
            name="Maria Hill"
        ):
            this.GainSurge(+1, effect)

    def executive_order_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        if Worlds.FindCardOnField(
            effect,
            name="Maria Hill"
        ):
            message.GainBoostIcon(3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            executive_order_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            executive_order_boost
        ),
    ]

