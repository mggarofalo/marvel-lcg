from . import *

# Web-Shot

def GetAbilities() -> Sequence['Ability']:

    def web_shot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 4, effect)
        if effect.GetPaidResources().HasColor("Y"):
            initiator.GetIdentity().ResolveSpecialAbility(initiator, effect, name="Venom Blast")


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            web_shot
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

