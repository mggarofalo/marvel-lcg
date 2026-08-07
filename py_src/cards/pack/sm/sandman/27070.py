from . import *

# Sandslide

def GetAbilities() -> Sequence['Ability']:

    def sandslide_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        city_streets = GetCityStreets(effect)
        if city_streets:
            Faces.PlaceCountersOn([city_streets], 2, 'sand', effect)

            def action():
                Faces.GiveStatus([player.GetIdentity()], "Stunned", effect)

            player = message.GetToPlayer()
            temp_effects = this.effect.RegisterTemp(
                AbilityFactory.AfterCardDiscardInternal(
                    AbilityType.Temp0,
                    None,
                    lambda effect, message:
                        action(),
                    conditions=[
                        lambda discard_effect, discard_message:
                            isinstance(discard_message.by_effect.bind_message, Message.WhenResolveSpecialAbility) and \
                            discard_message.by_effect.bind_message.by_effect == effect and \
                            discard_message.trigger.paper.IsFromSet("Sandman"),
                    ]
                ),
                unregister_after_exec=False,
                until_turn_end=True
            )
            ResolveSurgingSandsOnCityStreets(effect)

            Effect.UnRegisterSelf(*temp_effects)


    def sandslide_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.ResolveAbility([this], player, AbilityType.WhenRevealed, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sandslide_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sandslide_boost
        ),
    ]

