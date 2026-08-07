from . import *

# "You Stand Accused!"

def GetAbilities() -> Sequence['Ability']:

    def you_stand_accused_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=1))

    def you_stand_accused_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=1))


    def you_stand_accused_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            you_stand_accused_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            you_stand_accused_hero
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            you_stand_accused_boost
        ),
    ]

