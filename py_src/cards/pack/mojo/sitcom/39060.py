from . import *
from cards.pack.mojo import DiscardEachSettingAndGainSurge

# Mojo in the Middle

def GetAbilities() -> Sequence['Ability']:

    def mojo_in_the_middle(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Obligation,
            acceleration_icon=1,
        ),
        AbilityFactory.AfterCardDiscardInternal(
            AbilityType.Response,
            Obligation,
            mojo_in_the_middle,
            from_where="PlayerArea"
        ),
        DiscardEachSettingAndGainSurge()
    ]

