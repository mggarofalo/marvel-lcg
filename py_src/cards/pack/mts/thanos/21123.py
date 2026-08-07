from . import *

# The Mad Titan

def GetAbilities() -> Sequence['Ability']:

    def the_mad_titan_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = GetInfinityStone(effect).GetTopCard()
        if face:
            face.PutIntoPlay(player, effect)

    def the_mad_titan_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        def action(unit: 'Unit2'):
            face = GetInfinityStone(effect).GetTopCard()
            if face:
                face.PutIntoPlay(player, effect)
        message.would_atk_message.IfThisAttackDefeats(Ally, action, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_mad_titan_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            the_mad_titan_boost,
            during_attack=True
        ),
    ]

