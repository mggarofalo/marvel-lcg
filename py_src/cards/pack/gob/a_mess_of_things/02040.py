from . import *

# Tail Sweep

def GetAbilities() -> Sequence['Ability']:

    def tail_sweep_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        scorpion = Worlds.FindCardOnField(effect, name="Scorpion", card_type=Enemy)
        def action():
            identity = player.GetIdentity()
            Faces.GiveStatus([identity], "Stunned", effect)

        if scorpion:
            scorpion.DoAttackYou(player, effect, if_no_attack_was_made=action)
        else:
            action()

    def tail_sweep_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tail_sweep_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tail_sweep_boost
        ),
    ]

