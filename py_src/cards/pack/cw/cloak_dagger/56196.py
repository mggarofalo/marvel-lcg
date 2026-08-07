from . import *

# Darkforce

def GetAbilities() -> Sequence['Ability']:

    def darkforce(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        size = message.placed_threat
        this.RemoveThreatFromSchemes("YourLeaderMainScheme", size, effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "EnemyLeader",
            when_attach_operation=lambda face, effect:
                Faces.GiveStatus([face], "Tough", effect)
        ),
        AbilityFactory.AfterEnemySchemeAndPlaceThreat(
            AbilityType.ForcedResponse,
            "AttachedLeader",
            MainScheme,
            darkforce
        ),
    ]

