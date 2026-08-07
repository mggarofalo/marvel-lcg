from . import *

# * Morlun

def GetAbilities() -> Sequence['Ability']:

    def morlun_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            player.GetIdentity().TakeDamage(this, 2, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("INHERITOR", Minion),
            attack=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            morlun_revealed
        ),
    ]

