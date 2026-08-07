from . import *

# * Generation X

def GetAbilities() -> Sequence['Ability']:

    def generation_x(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.PlayerCard(
                effect,
                player,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=Event,
                card_class="IdentitySpecific",
                may=True
            )
            if face:
                player.GainCard(face, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.NonKeyword,
            CardFinder2("X-MEN", Unit2),
            "This",
            lambda effect, message:
                message.GainTHWForThisThwart(+1, effect),
            is_basic_thwart=True,
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            generation_x,
        ),
    ]

