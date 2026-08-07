from . import *

# Pheromones

def GetAbilities() -> Sequence['Ability']:

    def pheromones_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)
        Faces.GiveStatus([identity], "Confused", effect)

        PlaceSkillCounterOnFinesse(1, effect)

    def pheromones_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not Faces.GiveStatus([identity], "Stunned", effect):
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            pheromones_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            pheromones_boost
        ),
    ]

