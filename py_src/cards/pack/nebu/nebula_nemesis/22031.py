from . import *

# Old Rivals

def GetAbilities() -> Sequence['Ability']:

    def old_rivals_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        gamora = Worlds.FindCardOnField(
            effect,
            name="Gamora",
        )
        def action():
            ThisCardGainSurge(effect)

        if gamora:
            if Enemy.IsType(gamora):
                gamora.DoAttackYou(player, effect, if_no_attack_was_made=action)
            elif Hero.IsType(gamora) or Ally.IsType(gamora):
                gamora.BasicAttack([identity], effect, if_no_attack_was_made=action)
        else:
            action()

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            old_rivals_revealed
        ),
    ]

