from . import *

# Coordinated Attack

def GetAbilities() -> Sequence['Ability']:

    def coordinated_attack(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        if message.attacker:
            ally = message.attacker.CastTo(Ally)
            ally.GainForThisActive(effect, message, attack_consequential_damage=-1)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.NonKeyword,
            Ally,
            coordinated_attack,
            attack_targets="AttachedMinion"
        ),
    ]

