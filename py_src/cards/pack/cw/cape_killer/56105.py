from . import *

# Cape-Killer

def GetAbilities() -> Sequence['Ability']:

    def cape_killer(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.GainForThisActive(
            effect,
            message,
            attack=2,
        )


    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity, trait="UNREGISTERED"),
            cape_killer
        ),
    ]

