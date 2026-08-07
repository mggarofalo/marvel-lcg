from . import *

# Surprise!

def GetAbilities() -> Sequence['Ability']:

    def surprise_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        has_put_into_play = False
        def action():
            nonlocal has_put_into_play
            has_put_into_play = True

        this.effect.RegisterTemp(
            AbilityFactory.WhenCardEnterPlay(
                AbilityType.Temp0,
                Villain,
                lambda effect, message:
                    action(),
                conditions=[
                    lambda check_effect, check_message:
                        isinstance(check_message.by_effect.bind_message, Message.WhenResolveSpecialAbility) and \
                        effect == check_message.by_effect.bind_message.by_effect,
                ]
            ),
            unregister_after_exec=True,
            until_turn_end=True
        )
        ResolveMainSchemeAmbush(player, effect)

        if not has_put_into_play:
            scheme = GetLightAtTheEnd(effect)
            this.PlaceThreatOnSchemes([scheme], 3, effect)


    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            surge=1
        ),
        AbilityFactory.IfExpertModeThisCannotBeCanceled(),
        AbilityFactory.WhenThisRevealed(
            None,
            surprise_revealed
        ),
    ]

