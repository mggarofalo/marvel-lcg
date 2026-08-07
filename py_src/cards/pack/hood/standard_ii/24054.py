from . import *

# Total Annihilation

def GetAbilities() -> Sequence['Ability']:

    def total_annihilation_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(overkill=True))


    def total_annihilation_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.would_atk_message.GainOverKill(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Hero",
            total_annihilation_hero
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            total_annihilation_boost,
            during_attack=True,
            attacker=Villain,
        ),
    ]

