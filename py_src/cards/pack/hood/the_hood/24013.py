from . import *

# Upper Hand

def GetAbilities() -> Sequence['Ability']:

    def upper_hand_reveal_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoSchemes(player, effect)
        ResolveFoulPlayerAbility(player, effect)


    def upper_hand_reveal_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect)
        ResolveFoulPlayerAbility(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            upper_hand_reveal_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            upper_hand_reveal_hero
        ),
    ]

