from . import *

# High Fashion

def GetAbilities() -> Sequence['Ability']:

    def high_fashion_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindVillain(effect, "Electro"):
            player.DiscardControlCards(effect, control=True, highest_cost=True)
        else:
            PutSetasideVillainIntoPlay("Electro", effect)

        if Worlds.FindVillain(effect, "Kraven the Hunter"):
            player.DiscardControlCards(effect, control=True, lowest_cost=True)
        else:
            PutSetasideVillainIntoPlay("Kraven the Hunter", effect)


    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            incite=1
        ),
        AbilityFactory.IfExpertModeThisCannotBeCanceled(),
        AbilityFactory.WhenThisRevealed(
            None,
            high_fashion_revealed
        ),
    ]

