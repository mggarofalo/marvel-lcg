from . import *

# Armor Up

def GetAbilities() -> Sequence['Ability']:

    def armor_up(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().ChangeToForm(Hero, effect)


    return [
        AbilityFactory.WhenEnemyWouldActivate(
            AbilityType.AlterEgoInterrupt,
            Villain,
            armor_up
        ).SetPlay().SetLabel(),
    ]

