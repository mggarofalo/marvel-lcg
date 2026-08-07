from . import *

# The Unstoppable Juggernaut

def GetAbilities() -> Sequence['Ability']:

    def the_unstoppable_juggernaut(effect: 'Effect', message: 'Message.WhenMainSchemeStageWouldBeCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)

        this.RemoveThreatFromSchemes([this], "All", effect)

        face = Worlds.FindCardOnField(
            effect,
            name="Juggernaut Exposed",
            card_type=Attachment
        )
        if face:
            face.card.Flip(effect)

        villain = Worlds.FindCardOnField(
            effect,
            name="Juggernaut",
            card_type=Villain
        )
        if villain:
            Faces.PlaceCountersOn([villain], 1, 'momentum', effect)

            def action(player: 'Player'):
                villain.DoAttackYou(player, effect)

            Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenMainSchemeStageWouldBeCompleted(
            AbilityType.ForcedInterrupt,
            "This",
            the_unstoppable_juggernaut
        ),
    ]

