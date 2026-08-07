from . import *

# * The Shadow King

def GetAbilities() -> Sequence['Ability']:

    def the_shadow_king(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player:
            allies = player.GetControlAllies()
            allies = Filter.All(allies, Ally, highest_thw=True)

            if allies:
                player.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Discard that ally",
                        lambda targets:
                            Faces.DiscardAll(targets, effect)
                    ).SetTarget(allies, canbe_discard=True),
                    AbilityFactory.ForChoiceAbility(
                        "Place threat on the main scheme equal to its THW",
                        lambda targets:
                            this.PlaceThreatOnSchemes(targets, allies[0].thwart, effect),
                    ).SetTarget(MainScheme)
                )

    return [
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            the_shadow_king
        ),
    ]

