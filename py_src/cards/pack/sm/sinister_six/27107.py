from . import *

# Brute Force Barricade

def GetAbilities() -> Sequence['Ability']:

    def brute_force_barricade_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            SchemeSide2,
            conditions=[
                lambda effect, message:
                    effect.this != message.trigger
            ],
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            brute_force_barricade_boost
        ),
    ]

