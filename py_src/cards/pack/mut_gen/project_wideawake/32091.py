from . import *

# * Cannonball: Sam Guthrie

def GetAbilities() -> Sequence['Ability']:

    def cannonball(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GainForThisActive(
            effect,
            message.GetWouldAtkMessage(),
            attack_consequential_damage=-1
        )


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.NonKeywordStar,
            "This",
            Minion,
            cannonball,
        ),
    ]

