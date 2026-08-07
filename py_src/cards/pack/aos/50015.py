from . import *

# Agents of S.H.I.E.L.D.

def GetAbilities() -> Sequence['Ability']:

    def agents_of_shield(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        message.CancelAllEffectsAndDiscardIt(effect)
        Worlds.RevealEncounterDeckTopCard(initiator, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True,
        ),
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.Interrupt,
            "You",
            EncounterCard,
            "All",
            agents_of_shield
        ).SetCondition(each_your_characters_has_trait="S.H.I.E.L.D")
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

