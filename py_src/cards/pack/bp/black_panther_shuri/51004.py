from . import *

# On the Prowl

def GetAbilities() -> Sequence['Ability']:

    def on_the_prowl(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

        initiator.ResolveSpecialAbility(effect.targets2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            on_the_prowl
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Upgrade, trait="BLACK PANTHER", from_where=["YouControlCards"]),
    ]

