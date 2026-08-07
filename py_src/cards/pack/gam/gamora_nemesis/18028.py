from . import *

# Waylay

def GetAbilities() -> Sequence['Ability']:

    def waylay_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        gamora = Worlds.FindCardOnField(
            effect,
            card_type=Friend,
            name="Gamora"
        )
        if gamora:
            if gamora.IsConfused() or gamora.IsStunned():
                ThisCardGainSurge(effect)
            Faces.GiveStatus([gamora], "Stunned", effect)
            Faces.GiveStatus([gamora], "Confused", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            waylay_revealed
        ),
    ]

