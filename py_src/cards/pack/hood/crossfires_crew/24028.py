from . import *

# Caught in the Crossfire

def GetAbilities() -> Sequence['Ability']:

    def caught_in_the_crossfire_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="CROSSFIRE'S CREW",
        )
        if face:
            face.Reveal(player, effect)
        damage = len(Worlds.GetOnFieldMinions(effect, CardFinder2("CROSSFIRE'S CREW")))
        player.GetIdentity().TakeIndirectDamage(this, damage, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            caught_in_the_crossfire_revealed
        ),
    ]

