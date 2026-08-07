from . import *

# Manufactured Drama

def GetAbilities() -> Sequence['Ability']:

    def manufactured_drama(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        if not Faces.ExhaustAll(player.supports.Get(), effect):
            ThisCardGainSurge(effect)

    def manufactured_drama_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)

        Faces.DiscardAll([this], effect)

    return [
        *AbilityFactory.CardCannotReady(
            Support,
            control_by="You"
        ),
        AbilityFactory.WhenThisObligationRevealed(
            manufactured_drama
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            manufactured_drama_action
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", lambda effect:
            effect.GetInitiator().supports.GetSize()))
    ]

