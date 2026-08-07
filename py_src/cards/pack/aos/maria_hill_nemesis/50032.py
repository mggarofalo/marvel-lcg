from . import *

# Controlled Innocents

def GetAbilities() -> Sequence['Ability']:

    def controlled_innocents(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Controlled Minion"),
            base_sch=1,
            base_atk=1,
            base_health=1,
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            CardFinder2("CONTROLLED", Minion),
            controlled_innocents,
        )
    ]

