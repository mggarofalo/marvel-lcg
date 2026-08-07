from . import *

# * Avalanche

def GetAbilities() -> Sequence['Ability']:

    def avalanche_boost(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Take 2 indirect damage",
                    lambda targets:
                        player.GetIdentity().TakeIndirectDamage(this, 2, effect)
                ),
                AbilityFactory.ForChoiceAbility(
                    "Exhaust their identity",
                    lambda targets:
                        Faces.ExhaustAll(targets, effect),
                ).SetTarget("YourIdentity", canbe_exhaust=True),
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            avalanche_boost
        )
    ]

