from . import *

# Warp Reality

def GetAbilities() -> Sequence['Ability']:

    def warp_reality(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.CancelAllEffectsAndDiscardIt(effect)

        value = FacesCounter.CountTotalBoostIcons(effect.targets)
        Worlds.DiscardEncounterCards(value, effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            EncounterCard,
            "All",
            warp_reality,
            from_encounter=True,
        ).SetPlay().SetLabel()
        .SetTarget("Trigger"),
    ]

