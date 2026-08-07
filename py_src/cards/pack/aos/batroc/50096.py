from . import *

# Leaping Kick

def GetAbilities() -> Sequence['Ability']:

    def leaping_kick_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        batroc = Worlds.FindCardOnField(effect, name="Batroc", card_type=Enemy)
        if batroc:
            batroc.DoSchemes(player, effect)

    def leaping_kick_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        batroc = Worlds.FindCardOnField(effect, name="Batroc", card_type=Enemy)
        if batroc:
            ally = Filter.One(Worlds.GetOnFieldAllies(effect), effect, most_remaining_hp=True)
            if ally:
                batroc.BasicAttack([ally], effect, property=AttackProperty(overkill=True))
            else:
                batroc.DoAttackYou(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            leaping_kick_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            leaping_kick_hero
        ),
    ]

