from . import *

# Assault

def GetAbilities() -> Sequence['Ability']:

    def assault_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Hero",
            assault_hero
        ),
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        )
    ]
