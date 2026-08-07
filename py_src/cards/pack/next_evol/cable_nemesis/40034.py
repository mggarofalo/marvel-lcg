from . import *

# Telekinetic Force Field

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_force_field(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        damage = message.PreventDamage("All", effect)
        if damage >= 2:
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Stryfe"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            telekinetic_force_field
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            "ActivatingEnemy"
        ),
    ]

