from cards.pack import *

def GiveKlawAdditionalBoostCardWhenAttack():

    def give_additional_boost_card(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.GiveAdditionalBoostCardForThisActivation(1, effect)

    return AbilityFactory.WhenUnitWouldAttack(
        AbilityType.ForcedInterrupt,
        "This",
        give_additional_boost_card
    )

