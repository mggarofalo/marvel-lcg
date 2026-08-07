from . import *

# Piercing Thorns

def GetAbilities() -> Sequence['Ability']:

    def piercing_thorns_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)
        if AbsorbingManHasTrait(effect, 'WOOD'):
            player.DiscardControlCards(effect, control=True)


    def piercing_thorns_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        if AbsorbingManHasTrait(effect, 'STONE', 'WOOD'):
            player = message.GetToPlayer()
            Faces.GiveStatus([player.GetIdentity()], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            piercing_thorns_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            piercing_thorns_boost
        ),
    ]

