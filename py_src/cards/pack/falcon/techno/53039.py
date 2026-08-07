from . import *

# Jet Pack

def GetAbilities() -> Sequence['Ability']:

    def jet_pack(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.PreventDamage("All", effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Fixer"),
            if_cannot_attach_to=Villain
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Enemy,
            trait="AERIAL"
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            jet_pack,
            at_least_damage=3
        ),
    ]

