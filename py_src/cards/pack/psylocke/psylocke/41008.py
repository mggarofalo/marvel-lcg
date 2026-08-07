from . import *

# Training Regimen

def GetAbilities() -> Sequence['Ability']:

    def training_regimen(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="SKILL"
        )
        if face:
            initiator.GainCard(face, effect)

        if initiator.IsInHeroForm():
            initiator.DiscardHandCards((1, 1), effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            training_regimen
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

