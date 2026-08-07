from . import *

# Frequent Flyers

def GetAbilities() -> Sequence['Ability']:

    def frequent_flyers_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindVillain(effect, "Hobgoblin"):
            player.GetIdentity().TakeIndirectDamage(this, 2, effect)
        else:
            PutSetasideVillainIntoPlay("Hobgoblin", effect)

        if Worlds.FindVillain(effect, "Vulture"):
            player.DiscardRandomHandCards(1, effect)
        else:
            PutSetasideVillainIntoPlay("Vulture", effect)


    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            incite=1
        ),
        AbilityFactory.IfExpertModeThisCannotBeCanceled(),
        AbilityFactory.WhenThisRevealed(
            None,
            frequent_flyers_revealed
        ),
    ]

