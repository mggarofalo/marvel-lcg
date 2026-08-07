from . import *

# Stampede

def GetAbilities() -> Sequence['Ability']:

    def stampede_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Rhino",
            card_type=Villain
        )

        if villain:
            villain.DoAttackYou(player, effect,
                if_characters_damaged_by_this=lambda units:
                    Faces.GiveStatus(units, "Stunned", effect)
            )

    return [
        AbilityFactory.WhenThisRevealed(
            "Hero",
            stampede_hero
        ),
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        )
    ]

