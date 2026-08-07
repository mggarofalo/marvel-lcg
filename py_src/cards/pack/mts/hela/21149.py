from . import *

# Hela's Domain

def GetAbilities() -> Sequence['Ability']:

    def helas_domain_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = 1 + GetSideSchemesNumInVictoryDisplay(effect)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], value, effect)

    def helas_domain_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def place_threat(defeat_effect: 'Effect', defeat_message: 'Message.WhenUnitBeDefeated') -> None:
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 2, effect)

        if isinstance(message.being_message, Message.WhenUnitBeingAttack):
            this.effect.RegisterTemp(
                AbilityFactory.WhenUnitBeDefeated(
                    AbilityType.Temp0,
                    Ally,
                    place_threat,
                    by_attack=message.being_message.would_atk_message,
                ),
                unregister_after_exec=False,
                until_event_end=message.being_message.would_atk_message
            )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            helas_domain_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            helas_domain_boost
        ),
    ]

