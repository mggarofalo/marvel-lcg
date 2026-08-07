from . import *

# * White Fox: Ami Han

def GetAbilities() -> Sequence['Ability']:

    def white_fox(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            this.PutIntoPlay(player, effect)


    return [
        AbilityFactory.AfterCardDiscardInternal(
            AbilityType.Response,
            "This",
            white_fox,
            from_where="PlayerDeckTop",
        ).SetTarget("Initiator")
    ]

