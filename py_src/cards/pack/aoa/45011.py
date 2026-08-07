from . import *

# * Cable: Nathan Summers

def GetAbilities() -> Sequence['Ability']:

    def cable(effect: 'Effect', message: 'Message.AfterUnitDefeatedScheme') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.AfterUnitDefeatedScheme(
            AbilityType.Response,
            "This",
            SchemeSide2,
            cable,
            is_from_thwart=True
        ),
    ]

