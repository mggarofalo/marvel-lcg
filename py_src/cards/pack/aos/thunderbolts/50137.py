from . import *

# Down but Not Out

def GetAbilities() -> Sequence['Ability']:

    def down_but_not_out_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            ThisCardGainSurge(effect)

        minions = Worlds.VictoryDisplay(effect).FindCards(trait="THUNDERBOLT", card_type=Minion)
        if minions:
            minion = Rand.RandomChoice(minions, effect)
            minion.Reveal(player, effect, if_no_entered_play=action)

            minion.SetHealth(5, effect)

            Faces.RemoveAllFromGame([this], effect)
        else:
            action()

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            down_but_not_out_revealed
        ),
    ]

