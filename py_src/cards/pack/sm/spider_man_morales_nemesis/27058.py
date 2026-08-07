from . import *

# * Prowler

def GetAbilities() -> Sequence['Ability']:

    def prowler_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if player.IsAlterEgo():
            Faces.GiveStatus([this], "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            prowler_revealed
        ),
    ]

