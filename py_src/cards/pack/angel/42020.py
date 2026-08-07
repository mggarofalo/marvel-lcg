from . import *

# * Cannonball: Sam Guthrie

def GetAbilities() -> Sequence['Ability']:

    def cannonball(effect: 'Effect') -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        return -1 * initiator.hand_cards.FindCardSize(CardFinder2("AERIAL"))


    return [
        AbilityFactory.WhenAllyWouldTakeConsequentialDamage(
            "This",
            update_damage=cannonball
        ),
    ]

