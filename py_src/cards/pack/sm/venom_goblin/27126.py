from . import *

# Spreading Panic

def GetAbilities() -> Sequence['Ability']:

    def spreading_panic(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        schemes = Worlds.FindCardsOnField(
            effect,
            card_type=Scheme2,
            has_counter='glider'
        )
        for scheme in schemes:
            scheme.ResolveSpecialAbility(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spreading_panic
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            spreading_panic
        )
    ]

