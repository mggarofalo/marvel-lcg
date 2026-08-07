from cards.pack import *

def WhenEbonyMawActivatesAgainstYouRemoveAnInvocationCounterFromEachSpellCardInYourPlayArea():
    def ebony_maw(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.area_environment.FindCards(card_type=Environment, trait="SPELL")
        faces = player.ChooseOrder(faces)
        Faces.RemoveCountersOn(faces, 1, 'invocation', effect)

    return AbilityFactory.WhenEnemyActivateAgainstYou(
        AbilityType.ForcedInterrupt,
        "This",
        ebony_maw
    )

