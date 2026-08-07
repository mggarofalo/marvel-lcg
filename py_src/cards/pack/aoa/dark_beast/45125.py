from . import *

# Evil Genius

def GetAbilities() -> Sequence['Ability']:

    def evil_genius_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Dark Beast",
            card_type=Enemy
        )

        if villain:
            villain.DoSchemes(player, effect)
            Faces.GiveStatus([villain], "Tough", effect)


    def evil_genius_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Dark Beast",
            card_type=Enemy,
        )

        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_boost_card=1))


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            evil_genius_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            evil_genius_hero
        ),
    ]

