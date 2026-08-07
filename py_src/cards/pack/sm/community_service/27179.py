from . import *

# Off the Rails

def GetAbilities() -> Sequence['Ability']:

    def off_the_rails(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'speed', effect)

        if this.GetCounters('speed') >= 2:
            Faces.RemoveAllFromGame([this], effect)
            Players.ForEachPlayer(
                effect,
                lambda player:
                    player.DiscardDeckTopCards(3, effect)
            )


    return [
        AbilityFactory.WhenVillainPhaseBegin(
            AbilityType.ForcedInterrupt,
            off_the_rails
        ),
    ]

