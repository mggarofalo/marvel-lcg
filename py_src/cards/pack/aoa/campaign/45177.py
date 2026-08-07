from . import *

# North American Sea Wall

def GetAbilities() -> Sequence['Ability']:

    def north_american_sea_wall_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        player.DealEncounterCard(this, effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            Villain,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            north_american_sea_wall_boost
        ),
    ]

