from . import *

# * Jennifer Walters

def GetAbilities() -> Sequence['Ability']:

    def jennifer_walters(effect: 'Effect', message: 'Message.WhenSchemeWouldPlaceThreat') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        message.PreventThreat(1, effect)


    return [
        AbilityFactory.WhenThreatWouldBePlacedOn(
            AbilityType.Interrupt,
            None,
            jennifer_walters
        ).SetName('"I Object!"')
        .LimitOncePerRound(),
    ]

