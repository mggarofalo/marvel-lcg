from . import *

# Blackbird Pellets

def GetAbilities() -> Sequence['Ability']:

    def blackbird_pellets(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        player.DiscardRandomHandCards(1, effect)
        player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("MYSTIC", Minion),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            blackbird_pellets,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

