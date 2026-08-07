from . import *

# Psychological Manipulation

def GetAbilities() -> Sequence['Ability']:

    def psychological_manipulation_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if not player.DiscardControlCards(effect, ally=True, support=True):
            ThisCardGainSurge(effect)

    def psychological_manipulation_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetHero()

        units = Worlds.GetOnFieldFriendlyCharacters(effect)
        unit = Filter.One(units, effect, fewest_remaining_hp=True)
        value = identity.attack

        if unit:
            this.DealDamage([unit], value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            psychological_manipulation_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            psychological_manipulation_hero
        ),
    ]

