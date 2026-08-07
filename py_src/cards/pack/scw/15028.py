from . import *

# Browbeat

def GetAbilities() -> Sequence['Ability']:

    def browbeat(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = 2
        villain = Worlds.FindVillain(effect)
        if villain:
            damage += min(3, villain.printed_stage)

        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            browbeat
        ).SetPlay(only_if_your_identity_has_trait="AVENGER").SetLabel('attack')
        .SetTarget(Villain),
    ]

