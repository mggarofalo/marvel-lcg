from . import *

# Clash Of The Titans

def GetAbilities() -> Sequence['Ability']:

    def clash_of_the_titans(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        gain_surge = True

        enemy = Filter.One(Worlds.FindCardsOnField(effect, card_type=Enemy), effect, highest_atk=True)
        if enemy:
            friends: List[Hero|Ally] = Worlds.FindCardsOnField(effect, card_type=Hero|Ally)
            friend = Filter.One(friends, effect, highest_atk=True)

            if friend:
                if enemy.BasicAttack([friend], effect):
                    gain_surge = False

        if gain_surge:
            this.GainSurge(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            clash_of_the_titans
        )
    ]

