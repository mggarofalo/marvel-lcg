from . import *

# Smooth as Silk

def GetAbilities() -> Sequence['Ability']:

    def smooth_as_silk(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        set_name = effect.targets[0].paper.set_name

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            set_name=set_name
        )

        if face:
            TuckCardUnderSilk(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            smooth_as_silk
        ).SetPlay().SetLabel()
        .SetTarget("Enemy|Scheme"),
    ]

