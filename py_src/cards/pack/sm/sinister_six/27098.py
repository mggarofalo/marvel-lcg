from . import *

# * Scorpion

def GetAbilities() -> Sequence['Ability']:

    def scorpion_revealed(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.GiveStatus(targets, "Stunned", effect)
            ).SetTarget("YouControlUnit", canbe_stunned=True)
        )
        MoveTheActiveCounterToTheNextVillain(effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            scorpion_revealed,
        ),
        SinisterSixWhenDefeated(),
    ]

