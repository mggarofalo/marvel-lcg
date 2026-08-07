from . import *

# * Dust: Sooraya Qadir

def GetAbilities() -> Sequence['Ability']:

    def dust(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GainForThisActive(effect, message, attack_consequential_damage=+1)
        message.AlsoResolveThisAttackTo(effect.targets)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "This",
            dust,
            attack_targets=Minion
        ).SetTarget(Minion, range="All"),
    ]

