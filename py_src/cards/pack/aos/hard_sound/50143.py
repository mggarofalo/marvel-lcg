from . import *

# * Songbird

def GetAbilities() -> Sequence['Ability']:

    def songbird(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GiveAdditionalBoostCardForThisActivation(1, effect)


    return [
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            songbird
        ),
    ]

