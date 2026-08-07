from . import *

# Sinister Strike

def GetAbilities() -> Sequence['Ability']:

    def sinister_strike_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        env = GetPursuedByThePast(effect)
        if env:
            Faces.PlaceCountersOn([env], 1, 'pursuit', effect)
            if env.GetAllCounters():
                ThisCardGainSurge(effect)

    def sinister_strike_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        env = GetPursuedByThePast(effect)
        if env:
            Faces.PlaceCountersOn([env], 1, 'pursuit', effect)
            if env.GetAllCounters():
                villain = Worlds.FindVillain(effect)
                if villain:
                    villain.DoAttackYou(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            sinister_strike_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            sinister_strike_hero
        ),
    ]

