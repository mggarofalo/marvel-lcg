from . import *

# * Take Out the Guards

def GetAbilities() -> Sequence['Ability']:

    def take_out_the_guards(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Discard 1 non-[[ELITE]] minion from play",
                    lambda targets:
                        Faces.DiscardAll(targets, effect)
                ).SetTarget(Minion, non_trait="ELITE", canbe_discard=True),
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            take_out_the_guards,
        ),
    ]

