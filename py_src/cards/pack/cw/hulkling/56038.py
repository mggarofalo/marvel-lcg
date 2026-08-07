from . import *

# Armored Defense

def GetAbilities() -> Sequence['Ability']:

    def armored_defense(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        this.DealDamage(effect.targets, hero.defense, effect)

        def action():
            Faces.GiveStatus([hero], "Tough", effect)

        RunAt.AfterEventEnd(effect, message, action)

    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            armored_defense
        ).SetPlay().SetLabel('defense')
        .SetTarget("Attacker"),
    ]

