from . import *

# * Scarlet Spider: Kaine

def GetAbilities() -> Sequence['Ability']:

    def scarlet_spider(effect: 'Effect', message: 'Message.WhenCardWouldReveal') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        card_type = initiator.DeclareCardType()
        face = message.trigger
        if card_type.IsType(face):
            this.DealDamage([this], 1, effect)
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_you_control_trait="WEB-WARRIOR"),
        AbilityFactory.WhenCardWouldReveal(
            AbilityType.Interrupt,
            "You",
            EncounterCard,
            scarlet_spider
        ),
    ]

