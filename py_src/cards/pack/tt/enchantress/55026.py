from . import *

# Spell Shards

def GetAbilities() -> Sequence['Ability']:

    def spell_shards_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        def action(took_message: 'Message.AfterUnitTookDamage'):
            if took_message.trigger == identity and took_message.deal_damage >= 1:
                PlaceCharmCounter(player, 1, effect)

        identity.TakeIndirectDamage(this, 3, effect, operation=action)

    def spell_shards_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        PlaceCharmCounter(player, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spell_shards_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            spell_shards_boost
        ),
    ]

