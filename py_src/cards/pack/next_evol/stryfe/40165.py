from . import *

# * Stryfe

def GetAbilities() -> Sequence['Ability']:

    def stryfe(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        attacker = message.attacker

        player = attacker.GetControlByPlayer()
        value = FacesCounter.GetMostCommonType(player.hand_cards.Get())

        attacker.TakeDamage(this, value, effect)


    return [
        Stryfe(),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            Identity,
            "This",
            stryfe
        ),
    ]

