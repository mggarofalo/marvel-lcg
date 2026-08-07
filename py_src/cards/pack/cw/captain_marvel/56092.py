from . import *

# * Captain Marvel

def GetAbilities() -> Sequence['Ability']:

    def captain_marvel_setup(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        Find.FindAndAttachTo(
            effect,
            this,
            who_perform=team,
            name="Energy Channel"
        )

    def captain_marvel(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        PlaceEnergyCounterOnEnergyChannel(1, effect)


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            captain_marvel_setup
        ),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            "This",
            "Character",
            captain_marvel
        )
    ]

