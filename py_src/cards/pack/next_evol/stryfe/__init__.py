from cards.pack import *

def Stryfe() -> 'Ability':

    def stryfe(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()

        value = FacesCounter.GetMostCommonType(player.hand_cards.Get())
        message.GainATKForThisAttack(value, effect)

    return AbilityFactory.WhenUnitAttackYou(
        AbilityType.NonKeyword,
        "This",
        stryfe
    )

