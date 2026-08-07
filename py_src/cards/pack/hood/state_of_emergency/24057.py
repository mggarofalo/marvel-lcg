from . import *

# Offshore Inferno

def GetAbilities() -> Sequence['Ability']:

    def offshore_inferno_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, control=True, lowest_cost=True)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            offshore_inferno_revealed
        ),
    ]

