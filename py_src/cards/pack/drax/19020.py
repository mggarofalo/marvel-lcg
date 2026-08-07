from . import *

# * Gamora

def GetAbilities() -> Sequence['Ability']:

    def gamora(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckUntil(
            effect,
            card_type=Event
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN"),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.HeroResponse,
            "This",
            gamora
        ),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.HeroResponse,
            "This",
            gamora
        )
    ]

