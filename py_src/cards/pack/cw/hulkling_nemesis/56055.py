from . import *

# * Super Skrull

def GetAbilities() -> Sequence['Ability']:

    def super_skrull(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        face = player.DiscardDeckTopCard(effect)
        if HasResourceIcon.IsType(face):
            if face.printed_resource.y:
                message.GainATKForThisAttack(2, effect)
            if face.printed_resource.b:
                player.DiscardHandCards((1, 1), effect)
            if face.printed_resource.r:
                Faces.GiveStatus([this], "Tough", effect)
            if face.printed_resource.g:
                player.DiscardControlCards(effect, upgrade=True)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            super_skrull
        ),
    ]

