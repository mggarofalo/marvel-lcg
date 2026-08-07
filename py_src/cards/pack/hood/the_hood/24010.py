from . import *

# * Madame Masque

def GetAbilities() -> Sequence['Ability']:

    def madame_masque_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        ResolveFoulPlayerAbility(player, effect)


    def madame_masque_defeat(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            ResolveFoulPlayerAbility(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            madame_masque_revealed
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            madame_masque_defeat
        ),
    ]

