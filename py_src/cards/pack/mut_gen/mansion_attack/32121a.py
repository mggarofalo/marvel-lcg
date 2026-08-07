from . import *

# * Avalanche A

def GetAbilities() -> Sequence['Ability']:

    def avalanche(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.ExhaustAll(targets, effect)
                ).SetTarget("YourAlly", canbe_exhaust=True)
            )

    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            avalanche,
        ),
    ]

