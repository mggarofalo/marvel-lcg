from . import *

# * Bantam

def GetAbilities() -> Sequence['Ability']:

    def bantam_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Portal Through Time",
            card_type=SchemeSide2
        )
        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)
        else:
            player = message.GetToPlayer()
            Find.FindAndReveal(
                effect,
                player,
                name="Portal Through Time",
                card_type=SchemeSide2
            )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bantam_revealed
        ),
    ]

