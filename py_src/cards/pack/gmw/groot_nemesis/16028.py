from . import *

# Fan the Flames

def GetAbilities() -> Sequence['Ability']:

    def fan_the_flames_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        damage = 2
        furnax = Worlds.FindCardOnField(
            effect,
            name="Furnax"
        )
        blazing_inferno = Worlds.FindCardOnField(
            effect,
            name="Blazing Inferno"
        )
        if blazing_inferno:
            damage += 1
        if furnax:
            damage += 1
        identity.TakeIndirectDamage(this, damage, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            fan_the_flames_revealed
        ),
    ]

