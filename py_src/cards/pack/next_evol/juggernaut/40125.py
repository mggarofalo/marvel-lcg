from . import *

# Breakthrough

def GetAbilities() -> Sequence['Ability']:

    def breakthrough_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        juggernaut = Worlds.FindCardOnField(effect, name="Juggernaut", card_type=HasAttack)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take damage equal to Juggernaut's ATK",
                lambda targets:
                    identity.TakeDamage(this, juggernaut.attack, effect),
            ).SetTarget("YourIdentity") if juggernaut else None,
            AbilityFactory.ForChoiceAbility(
                "Discard the highest-cost upgrade or support you control",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget("Upgrade|Support", highest_cost=True, from_where=["YouControlCards"], canbe_discard=True)
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            breakthrough_revealed
        ),
    ]

