from . import *

# * Going Undercover

def GetAbilities() -> Sequence['Ability']:

    def going_undercover(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        faces = player.LookAtDeck("EncounterDeck", 5, effect)
        non_scenario_specific_faces = CardFinder(is_scenario_specific=False).Checks(faces, effect)

        def action(targets: Sequence[CardFace]):
            Faces.AddToVictoryDisplay(targets, effect)

            rest = [x for x in faces if x not in targets]
            player.PlaceOnTopAndOrBottomInAnyOrder(rest, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Add 1 non-scenario-specific card from among those to the victory display",
                action
            ).SetTarget(non_scenario_specific_faces, not_move=True)
        )


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            going_undercover,
            has_defeating_player=True
        ),
    ]

