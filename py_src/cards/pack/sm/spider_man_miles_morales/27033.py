from . import *

# Swing In

def GetAbilities() -> Sequence['Ability']:

    def swing_in(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 4, effect)
        if effect.GetPaidResources().HasColor("B"):
            initiator.GetIdentity().ResolveSpecialAbility(initiator, effect, name="Spider Camouflage")

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            swing_in
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

