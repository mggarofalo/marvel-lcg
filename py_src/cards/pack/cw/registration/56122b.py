from . import *

# Public Outrage

def GetAbilities() -> Sequence['Ability']:

    def public_outrage(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DiscardDeckTopCards(3, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.ThisGainKeyword(
            check_fn=lambda effect, ui:
                Worlds.GetYourTeamSize(effect) > 1,
            hinder="2*",
        ),
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            public_outrage,
        ),
    ]

