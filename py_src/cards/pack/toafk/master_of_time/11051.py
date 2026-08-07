from . import *

# Ancient Grudge

def GetAbilities() -> Sequence['Ability']:

    def ancient_grudge_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        kang = Worlds.FindCardOnField(
            effect,
            name="Kang (Master of Time)",
            card_type=Enemy
        )
        if kang:
            kang.DoActivate(player, effect)
        else:
            kang = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                name="Kang (Master of Time)",
            )
            if kang:
                kang.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ancient_grudge_revealed
        ),
    ]

