from . import *

# Superpower Training

def GetAbilities() -> Sequence['Ability']:

    def superpower_training(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.PlayerCard(
                effect,
                player,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=Upgrade,
                card_class="IdentitySpecific",
                may=True,
            )
            if face:
                face.PutIntoPlay(player, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            superpower_training,
        ),
    ]

