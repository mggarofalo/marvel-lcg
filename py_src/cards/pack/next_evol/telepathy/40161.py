from . import *

# Sowing Discord

def GetAbilities() -> Sequence['Ability']:

    def sowing_discord(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        if not Faces.ExhaustAll(player.GetControlAllies(), effect):
            ThisCardGainSurge(effect)

    def sowing_discord_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)

        Faces.DiscardAll([this], effect)

    return [
        *AbilityFactory.CardCannotReady(
            Ally,
            control_by="You"
        ),
        AbilityFactory.WhenThisObligationRevealed(
            sowing_discord
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            sowing_discord_action
        ).SetCost(Cost("BB"))
    ]

