from . import *

# * Venom Goblin (I)

def GetAbilities() -> Sequence['Ability']:

    def venom_goblin_i(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()

        scheme = MoveGliderToTheMainSchemeWithLeastThreat(player, effect, False)

        def place_2_threat_on_that_scheme(targets: Sequence['CardFace']):
            this.PlaceThreatOnSchemes(targets, 2, effect)

        def resolve_its_special_ability(targets: Sequence['CardFace']):
            scheme.ResolveSpecialAbility(player, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 2 threat on that scheme",
                place_2_threat_on_that_scheme
            ).SetTarget([scheme]),
            AbilityFactory.ForChoiceAbility(
                'resolve its "Special" ability',
                resolve_its_special_ability
            ),
        )


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            venom_goblin_i,
        ).SetName("Infest the City")
    ]

