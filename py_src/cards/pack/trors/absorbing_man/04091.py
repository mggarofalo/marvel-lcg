from . import *

# Avalanche!

def GetAbilities() -> Sequence['Ability']:

    def avalanche_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            delay = scheme.GetCounters('delay')
        else:
            delay = 0
        if delay >= 5:
            damage = 3
        else:
            damage = 2
        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbilityWithCost(
                    Cost("Y")
                ),
                AbilityFactory.ForChoiceAbility(
                    f"Take {damage} indirect damage",
                    lambda targets:
                        player.GetIdentity().TakeIndirectDamage(this, damage, effect)
                )
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            avalanche_revealed
        ),
    ]

