from . import *

# * Pip the Troll

def GetAbilities() -> Sequence['Ability']:

    def pip_the_troll(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for target in message.attacked_targets:
            player = target.GetControlByPlayer()
            this.PutIntoPlay(player, effect)
            break

    return [
        AbilityFactory.WhenUnitWouldBeAttacked(
            AbilityType.Interrupt,
            Identity,
            pip_the_troll,
        ).SetCost(Cost("YB"))
        .CanWorkOnlyInHand(),
    ]

