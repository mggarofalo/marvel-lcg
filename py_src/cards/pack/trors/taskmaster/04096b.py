from . import *

# Hunting Down Heroes

def GetAbilities() -> Sequence['Ability']:

    def hunting_down_heroes(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            if player.IsInHeroForm():
                player.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Place 1 threat here",
                        lambda targets:
                            this.PlaceThreatOnSchemes([this], 1, effect)
                    ).SetTarget("This"),
                    AbilityFactory.ForChoiceAbility(
                        "Take 1 damage",
                        lambda targets:
                            player.GetHero().TakeDamage(this, 1, effect)
                    ).SetTarget("YourIdentity")
                )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            hunting_down_heroes
        )
    ]

