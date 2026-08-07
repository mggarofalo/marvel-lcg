from . import *

# * Cosmo

def GetAbilities() -> Sequence['Ability']:

    def cosmo(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        card_type = initiator.DeclareCardType()
        face = effect.targets[0]
        Faces.DiscardAll([face], effect)
        if card_type.IsType(face):
            message.DoesNotTakeConsequentialDamage(effect)


    return [
        AbilityFactory.WhenUnitWouldAttackOrThwart(
            AbilityType.Interrupt,
            "This",
            cosmo
        ).SetTarget(from_where=["PlayersDeckTop", "EncounterDeckTop"]),
    ]

