from . import *

# Razor Dive

def GetAbilities() -> Sequence['Ability']:

    def razor_dive(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        overkill = False
        piercing = False

        if initiator.GetIdentity().IsName("Archangel"):
            overkill = True
            piercing = True

        this.DealDamage(effect.targets, 6, effect, property=AttackProperty(overkill=overkill, piercing=piercing))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            razor_dive
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

