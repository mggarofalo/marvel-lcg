from . import *

# High Ground

def GetAbilities() -> Sequence['Ability']:

    def high_ground_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        for face in Worlds.GetOnFieldFriendlyCharacters(effect):
            face.DiscardAllTough(effect)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect)
        if villain and villain.HasTrait("BRUTE"):
            damage = 4
        else:
            damage = 2

        player.GetIdentity().TakeIndirectDamage(this, damage, effect, as_group=True)

    def high_ground_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        if message.would_atk_message.attacker and \
            Villain.IsType(message.would_atk_message.attacker):
            message.would_atk_message.GainPiercing(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            high_ground_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            high_ground_boost,
            during_attack=True
        ),
    ]

