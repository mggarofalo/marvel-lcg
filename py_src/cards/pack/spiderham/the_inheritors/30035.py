from . import *

# * Karn

def GetAbilities() -> Sequence['Ability']:

    def karn_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            player.DiscardControlCards(effect, upgrade=True, support=True)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder2("INHERITOR", Minion),
            overkill=True,
            piercing=True,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            karn_revealed
        ),
    ]

