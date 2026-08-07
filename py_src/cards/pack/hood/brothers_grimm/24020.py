from . import *

# Corrosive Egg Bomb

def GetAbilities() -> Sequence['Ability']:

    def corrosive_egg_bomb(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        identity = player.GetIdentity()
        identity.TakeIndirectDamage(this, 3, effect)
        player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("MYSTIC", Minion),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            corrosive_egg_bomb,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

