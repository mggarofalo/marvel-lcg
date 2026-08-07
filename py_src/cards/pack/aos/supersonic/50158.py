from . import *

# Heat-Seeking Missiles

def GetAbilities() -> Sequence['Ability']:

    def heat_seeking_missiles(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'missile', effect)

        player = message.GetAgainstPlayer()
        if player:
            player.GetIdentity().TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="MACH-IV"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            heat_seeking_missiles
        ),
    ]

