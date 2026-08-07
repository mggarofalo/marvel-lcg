from . import *

# Imperial Guardsman

def GetAbilities() -> Sequence['Ability']:

    def imperial_guardsman(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetKillerPlayer()
        ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            if_cannot_gain_surge=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=4,
            trait="IMPERIAL GUARD",
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            imperial_guardsman
        ),
    ]

