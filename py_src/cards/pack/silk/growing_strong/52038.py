from . import *

# Titanic Proportions

def GetAbilities() -> Sequence['Ability']:

    def titanic_proportions_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        value = 0
        face = Worlds.FindCardOnField(effect, name="Atlas", card_type=Enemy)
        if face:
            value = face.GetCounters('growth')
        
        player.GetIdentity().TakeIndirectDamage(this, value, effect, as_group=True)

        if value < 3:
            ThisCardGainSurge(effect)

    def titanic_proportions_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Atlas", card_type=Enemy)
        if face:
            Faces.PlaceCountersOn([face], 1, 'growth', effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            titanic_proportions_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            titanic_proportions_boost
        ),
    ]

