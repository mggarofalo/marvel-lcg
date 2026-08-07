from . import *

# Hydra Reinforcements

def GetAbilities() -> Sequence['Ability']:

    def hydra_reinforcements(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(Minion, non_trait="ELITE", canbe_discard=True)
        )

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hydra_reinforcements,
            has_defeating_player=True
        ),
    ]

