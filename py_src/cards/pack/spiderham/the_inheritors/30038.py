from . import *

# * Verna

def GetAbilities() -> Sequence['Ability']:

    def verna_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("INHERITOR", Minion),
            retaliate=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            verna_revealed
        ),
    ]

