from . import *

# Crystal Ball

def GetAbilities() -> Sequence['Ability']:

    def crystal_ball(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.killer.GetControlBy()
        if Player.IsType(player):
            player.PlayOneCardLikeInTurn(player.hand_cards.Get(), effect, update_resources_cost=-3)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            crystal_ball,
        ),
    ]

