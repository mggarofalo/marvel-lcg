from . import *

# Psionic Shield

def GetAbilities() -> Sequence['Ability']:

    def psionic_shield(effect: 'Effect', message: 'Message.WhenCardWouldLeavePlay') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)

        minion = this.GetBindFace().CastTo(Minion)
        player = minion.engaged_player
        if player == None:
            player = "FirstPlayer"
        this.HealthUnits([minion], "All", effect)
        # minion.PutIntoPlay(player, effect)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            if_cannot_gain_surge=True
        ),
        AbilityFactory.WhenCardWouldLeavePlay(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            psionic_shield,
        ),
    ]

