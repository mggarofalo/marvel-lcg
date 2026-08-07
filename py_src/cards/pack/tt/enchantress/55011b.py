from . import *

# Trance of Wrath

def GetAbilities() -> Sequence['Ability']:

    def trance_of_wrath_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        face = Search.EncounterCardTop(
            effect,
            player,
            5,
        )
        if face:
            face.Reveal(player, effect)

    def trance_of_wrath(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        identity.TakeIndirectDamage(this, 1, effect)
        this.DealDamage(effect.targets2, 2, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="ENTHRALLED"
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            trance_of_wrath_revealed
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            trance_of_wrath
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This")
        .SetTarget2(Minion)
    ]

