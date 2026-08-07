from . import *

# "I Have You Now!"

def GetAbilities() -> Sequence['Ability']:

    def i_have_you_now_reveal_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.ExhaustAll([identity], effect)
        
        collector = Worlds.FindVillain(effect)
        if collector:
            collector.DoSchemes(player, effect)

    def i_have_you_now_reveal_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.GiveStatus([player.GetIdentity()], "Stunned", effect)
        collector = Worlds.FindVillain(effect)
        if collector:
            collector.DoAttackYou(player, effect)

    def i_have_you_now_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        collector = Worlds.FindVillain(effect)
        if collector:
            Faces.GiveStatus([collector], "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            i_have_you_now_reveal_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            i_have_you_now_reveal_hero
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            i_have_you_now_boost
        ),
    ]

