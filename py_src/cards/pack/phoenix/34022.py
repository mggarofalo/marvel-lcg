from . import *

# * Cerebro

def GetAbilities() -> Sequence['Ability']:

    def cerebro(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        
        if initiator.IsControl(CardFinder2("PSIONIC", Unit2)):
            face = Search.PlayerCard(
                effect,
                initiator,
                include_player_deck=True,
                card_type=Ally,
                trait="X-MEN",
            )
        else:
            face = Search.PlayerDeckTop(
                effect,
                initiator,
                5,
                card_type=Ally,
                trait="X-MEN",
            )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="MUTANT"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            cerebro
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

