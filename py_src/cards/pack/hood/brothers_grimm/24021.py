from . import *

# Paralytic Stardust

def GetAbilities() -> Sequence['Ability']:

    def paralytic_stardust(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Stunned", effect)
        player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("MYSTIC", Minion),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            paralytic_stardust,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

