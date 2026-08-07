from . import *

# * Get the Scoop

def GetAbilities() -> Sequence['Ability']:

    def get_the_scoop(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        this.RemoveThreatFromSchemes([this], 2, effect)

    def get_the_scoop_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        player = Worlds.FindPlayerByName("Cindy Moon", effect)
        if player:
            faces = player.LookAtDeck("EncounterDeck", 2, effect)

            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Tucks 1 of those cards",
                    lambda targets:
                        player.GetIdentity().TuckCardUnderHere(targets, effect)
                ).SetTarget(faces, not_move=True)
            )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            get_the_scoop
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
        .AnyPlayerCanDoThis(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            get_the_scoop_defeated,
        ),
    ]

