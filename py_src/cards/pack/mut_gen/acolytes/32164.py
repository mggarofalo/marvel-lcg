from . import *

# Zeal for the Cause

def GetAbilities() -> Sequence['Ability']:

    def zeal_for_the_cause_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minions = player.GetEngagedMinions(CardFinder2("ACOLYTE", Minion))
        if minions:
            Faces.ResolveAbility(minions, player, AbilityType.WhenDefeated, effect)
        else:
            minion = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion,
            )
            if minion:
                minion.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            zeal_for_the_cause_revealed
        ),
    ]

