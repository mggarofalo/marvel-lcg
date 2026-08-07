from . import *

# Trample

def GetAbilities() -> Sequence['Ability']:

    def trample_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        identity.TakeIndirectDamage(this, 2, effect)

    def trample_revealed_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Juggernaut",
            card_type=Villain
        )

        if villain:
            allies = Worlds.GetOnFieldAllies(effect)
            ally = Filter.One(allies, effect, fewest_remaining_hp=True)
            if ally:
                villain.BasicAttack([ally], effect)

    def trample_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.DealDamage(targets, 1, effect)
            ).SetTarget("YourAlly")
        )

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            trample_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            trample_revealed_hero
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            trample_boost
        ),
    ]

