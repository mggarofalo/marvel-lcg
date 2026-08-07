from . import *

# * Keep Up the Pressure

def GetAbilities() -> Sequence['Ability']:

    def keep_up_the_pressure(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.PlayerCard(
                effect,
                player,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=Event,
                trait="ATTACK",
                may=True
            )
            if face:
                player.GainCard(face, effect)

        Players.ForEachPlayer(effect, action)

        this.effect.RegisterTemp(
            AbilityFactory.WhenFaceWouldDealDamage(
                AbilityType.Temp0,
                None,
                lambda effect, message:
                    message.IncreaseDamage(+1, effect),
                by_play_card=CardFinder2("ATTACK", Event),
                allow_zero_damage=True
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            keep_up_the_pressure,
        ),
    ]

