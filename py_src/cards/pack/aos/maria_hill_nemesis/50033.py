from . import *

# Diabolical Discs

def GetAbilities() -> Sequence['Ability']:

    def diabolical_discs_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.RemoveCountersOn(targets, 1, 'all-purpose', effect)
            ).SetTarget(Support)
        )

        face = Worlds.FindCardOnField(
            effect,
            name="Controlled Innocents"
        )
        if face:
            Worlds.Enemies.PutYouDeckTopCardAsFacedownMinion(player, "Controlled Minion", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            diabolical_discs_revealed
        ),
    ]

