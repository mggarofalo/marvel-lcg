from . import *

# Nowhere is Safe

def GetAbilities() -> Sequence['Ability']:

    def nowhere_is_safe_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        env = GetPursuedByThePast(effect)
        if env:
            Faces.PlaceCountersOn([env], 1, 'pursuit', effect)
            if env.GetAllCounters() > 0:
                player.DiscardControlCards(effect, upgrade=True, support=True)

    def nowhere_is_safe_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            env = GetPursuedByThePast(effect)
            if env:
                Faces.PlaceCountersOn([env], 1, 'pursuit', effect)
        message.AfterThisActivation(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            nowhere_is_safe_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            nowhere_is_safe_boost
        ),
    ]

