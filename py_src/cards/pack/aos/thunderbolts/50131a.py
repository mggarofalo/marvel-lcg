from . import *

# Justice, Like Lightning

def GetAbilities() -> Sequence['Ability']:

    def justice_like_lightning_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        all_minions: List[CardFace] = []
        def action(player: 'Player'):
            all_minions.append(RevealRandomSetasideThunderboltMinion(player, effect))

        Players.ForEachPlayer(effect, action)

        minions = GetRemainingSetasideThunderboltMinion(effect)
        all_minions.extend(minions)
        for minion in minions:
            minion.Reveal(None, effect)
            minion.AttachTo2(this, effect)

        if Worlds.IsExpert(effect):
            Faces.GiveStatus(all_minions, "Tough", effect)

        this.card.Flip(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            justice_like_lightning_revealed
        ),
    ]

