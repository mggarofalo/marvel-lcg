from . import *

# * Scarlet Witch

def GetAbilities() -> Sequence['Ability']:

    def scarlet_witch(effect: 'Effect', message: 'Message.WhenBoostIconsWouldBeCount') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        message.SetBeInstead(effect)

        Faces.DiscardAll(effect.targets, effect)
        value = FacesCounter.CountTotalBoostIcons(effect.targets)
        message.SetBoostIcon(value, effect)


    return [
        AbilityFactory.WhenBoostIconsWouldBeCount(
            AbilityType.Interrupt,
            scarlet_witch
        ).SetTarget(from_where=["EncounterDeckTop"])
        .SetName("Chaos Control")
        .LimitOncePerPhase()
    ]

