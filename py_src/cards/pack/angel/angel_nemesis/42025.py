from . import *

# * Harpoon

def GetAbilities() -> Sequence['Ability']:

    def harpoon_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        face = player.DiscardDeckUntil(
            effect,
            card_type=Event,
        )
        if face:
            damage = FacesCounter.GetPrintedCost([face])
            player.GetIdentity().TakeIndirectDamage(this, damage, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            harpoon_revealed
        ),
        *AbilityFactory.UnitGetATKWhileAttacking(
            AbilityType.NonKeywordStar,
            "This",
            CardFinder2("AERIAL", Unit2),
            1
        ),
    ]

