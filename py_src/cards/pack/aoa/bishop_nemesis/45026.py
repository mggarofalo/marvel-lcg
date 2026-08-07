from . import *

# * Trevor Fitzroy

def GetAbilities() -> Sequence['Ability']:

    def trevor_fitzroy(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Portal Through Time",
            card_type=SchemeSide2
        )
        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)
        else:
            player = message.target.GetControlByPlayer()
            Find.FindAndReveal(
                effect,
                player,
                name="Portal Through Time",
                card_type=SchemeSide2
            )

    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            "This",
            Ally,
            trevor_fitzroy,
        ),
    ]

