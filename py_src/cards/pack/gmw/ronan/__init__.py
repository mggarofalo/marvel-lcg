from cards.pack import *
from cards.pack.gmw import *
from cards.pack.gmw.power_stone import *

def GiveAdditionalBoostCardIfYouControlPowerStone():

    def ronan_the_accuser_revealed(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        if player.IsControlAttachment(CardFinder(name="Power Stone")):
            message.GiveAdditionalBoostCardForThisActivation(1, effect)

    return AbilityFactory.WhenEnemyActivateAgainstYou(
        AbilityType.ForcedInterrupt,
        "This",
        ronan_the_accuser_revealed
    )