from . import *

# * Spiral

def GetAbilities() -> Sequence['Ability']:

    def spiral(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.DoSchemeInstead(effect)

    return [
        AbilityFactory.ThisCannotTakeDamageWhile(
            conditions=True
        ),
        AbilityFactory.UnitCannotBeStunned(
            "This"
        ),
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            MainScheme,
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "This",
            spiral,
        ),
    ]

