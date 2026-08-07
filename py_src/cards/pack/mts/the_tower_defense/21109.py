from . import *

# Rain Fire

def GetAbilities() -> Sequence['Ability']:

    def rain_fire_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        DealDamageToAvengersTower(3, effect)

    def rain_fire_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(unit: 'CardFace'):
            DealDamageToAvengersTower(3, effect)
        message.would_atk_message.IfThisAttackDefeats(Ally, action, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            rain_fire_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            rain_fire_boost,
            during_attack=True
        ),
    ]

