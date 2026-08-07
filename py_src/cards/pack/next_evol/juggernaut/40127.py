from . import *

# Ground Pound

def GetAbilities() -> Sequence['Ability']:

    def ground_pound_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        villain = Worlds.FindCardOnField(
            effect,
            name="Juggernaut",
            card_type=Villain
        )
        if villain:
            identity.TakeIndirectDamage(this, villain.attack, effect, as_group=True)

    def ground_pound_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        identity.TakeIndirectDamage(this, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ground_pound_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ground_pound_boost
        ),
    ]

