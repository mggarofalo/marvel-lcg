from . import *

# Web-Trap

def GetAbilities() -> Sequence['Ability']:

    def web_trap(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)
        if GetResourceGeneratedBySyncRatio(effect):
            Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            web_trap
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

