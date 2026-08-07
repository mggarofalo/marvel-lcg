from . import *

# * Horn of Proteus

def GetAbilities() -> Sequence['Ability']:

    def horn_of_proteus(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveStatus([message.trigger], "Tough", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Namor"),
            otherwise_attach_to="EnemyLeader"
        ),
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            horn_of_proteus
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BR"))
    ]

