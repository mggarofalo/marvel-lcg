from . import *

# * The Locust: Fernanda Rodriguez

def GetAbilities() -> Sequence['Ability']:

    def the_locust(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="CHAMPION"),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            the_locust
        ).SetTarget(Event, card_class="Aggression", from_where=["YourDiscardPile"]),
    ]

