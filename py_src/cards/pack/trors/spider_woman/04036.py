from . import *

# Pheromones

def GetAbilities() -> Sequence['Ability']:

    def pheromones(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)
        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            pheromones
        ).SetPlay().SetLabel()
        .SetTarget(Enemy, canbe_status=["Stunned", "Confused"]),
    ]

