from . import *

# Nano-Sentinel Tech

def GetAbilities() -> Sequence['Ability']:

    def nano_sentinel_tech(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        minion = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            include_set_aside=True,
            card_type=Minion,
            is_nemesis=player
        )
        if minion:
            minion.PutIntoPlay(player, effect)
            this.AttachTo2(minion, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            nano_sentinel_tech
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=4,
            trait="SENTINEL",
        ),
    ]

