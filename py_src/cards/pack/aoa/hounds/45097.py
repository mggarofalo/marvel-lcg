from . import *

# * Ahab

def GetAbilities() -> Sequence['Ability']:

    def ahab_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        scheme = Worlds.FindCardOnField(
            effect,
            name="Release the Hounds",
            card_type=Scheme2
        )
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 3, effect)
        else:
            player = message.GetToPlayer()
            Find.FindAndReveal(
                effect,
                player,
                name="Release the Hounds"
            )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ahab_revealed
        ),
    ]

