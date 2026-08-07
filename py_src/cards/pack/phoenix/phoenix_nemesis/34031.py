from . import *

# Fiery Rage

def GetAbilities() -> Sequence['Ability']:

    def fiery_rage_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Dark Phoenix",
            card_type=Enemy
        )
        if face:
            face.DoActivate(player, effect)
        else:
            scheme = Worlds.FindCardOnField(
                effect,
                name="Consume the World",
                card_type=Scheme2
            )
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 1, effect)
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            fiery_rage_revealed
        ),
    ]

