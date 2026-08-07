from . import *

# * Captain Marvel

def GetAbilities() -> Sequence['Ability']:

    def captain_marvel_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        Utility.DealEachPlayerEncounterCard(effect)
        Utility.CannotTakeDamageThisPhase(this, effect)


    def captain_marvel(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        PlaceEnergyCounterOnEnergyChannel(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            captain_marvel_revealed
        ),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            "This",
            "Character",
            captain_marvel
        )
    ]

