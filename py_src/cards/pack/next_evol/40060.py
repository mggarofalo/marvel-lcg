from . import *

# Digging Deep

def GetAbilities() -> Sequence['Ability']:

    def digging_deep(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(this, effect)


    return [
        AbilityFactory.AfterCardDiscardInternal(
            AbilityType.Response,
            "This",
            digging_deep,
            from_where="PlayerDeckTop",
        )
    ]

