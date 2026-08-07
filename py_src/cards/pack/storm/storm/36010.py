from . import *

# Torrential Rain

def GetAbilities() -> Sequence['Ability']:

    def torrential_rain(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

        initiator = effect.GetInitiator()
        hurricane = Worlds.FindCardOnField(
            effect,
            name="Hurricane",
            card_type=Support
        )
        if hurricane:
            hurricane.ResolveSpecialAbility(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            torrential_rain
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

