from . import *

# * Ultron (I)

def GetAbilities() -> Sequence['Ability']:

    def ultron(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()

        # Do we need this?
        # if target.GetPlayer().GetUnit().IsDeath():
        #     return

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 1 threat on the main scheme",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 1, effect)
            ).SetTarget(MainScheme, can_place_threat=True),
            AbilityFactory.ForChoiceAbility(
                "Put the top card of your deck into play facedown, engaged with you as a Drone minion",
                lambda targets:
                    CreateUltronFacedownDrone(player, 1, effect)
            )
        )


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            ultron
        )
    ]
