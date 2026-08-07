from . import *

# Hound

def GetAbilities() -> Sequence['Ability']:

    def hound_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if player.IsHero():
            this.DoAttackYou(player, effect)
        else:
            identity.ChangeToForm(Hero, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hound_revealed
        ),
    ]

