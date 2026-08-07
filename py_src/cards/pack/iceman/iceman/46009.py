from . import *

# Arctic Attack

def GetAbilities() -> Sequence['Ability']:

    def arctic_attack_4(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)
        initiator = effect.GetInitiator()
        face = GetSetAsideFrostbite(initiator)
        if face:
            face.AttachTo2(effect.targets[0], effect)

    def arctic_attack_6(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 6, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            arctic_attack_4,
            conditions=[
                lambda effect, message:
                    GetSetAsideFrostbite(effect.GetInitiator()) != None
            ]
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetName("Deal 4 damage to an enemy and attach a set-aside copy of Frostbite to it"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            arctic_attack_6
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy, with_attach=CardFinder(name="Frostbite"))
        .SetName("Deal 6 damage to an enemy with Frostbite attached"),
    ]

