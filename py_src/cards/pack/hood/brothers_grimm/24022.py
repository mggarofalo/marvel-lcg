from . import *

# Unbreakable Thread

def GetAbilities() -> Sequence['Ability']:

    def unbreakable_thread(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        player.DiscardControlCards(effect, ally=True, support=True, upgrade=True)
        player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("MYSTIC", Minion),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            unbreakable_thread,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

