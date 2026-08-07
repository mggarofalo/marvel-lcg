from . import *

# "I'll Take That"

def GetAbilities() -> Sequence['Ability']:

    def ill_take_that_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect)

        if villain and villain.HasTrait("PSIONIC"):
            lowest_cost = False
            highest_cost = True
        else:
            lowest_cost = True
            highest_cost = False

        if not player.DiscardControlCards(effect, upgrade=True, lowest_cost=lowest_cost, highest_cost=highest_cost):
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ill_take_that_revealed
        ),
    ]

