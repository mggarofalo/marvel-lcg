from . import *

# * Baron Mordo

def GetAbilities() -> Sequence['Ability']:

    def baron_mordo(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        players = Types.RemoveDuplicates([x.GetControlByPlayer() for x in message.attacked_targets])

        for player in players:
            identity = player.GetIdentity()
            face = player.DiscardDeckTopCard(effect)

            res = FacesCounter.GetPrintedResources([face])
            if res.r:
                Faces.GiveStatus([identity], "Stunned", effect)
            if res.y:
                identity.TakeDamage(this, 2, effect)
            if res.b:
                Faces.GiveStatus([identity], "Confused", effect)
            if res.g:
                Faces.GiveStatus([identity], "Stunned", effect)
                identity.TakeDamage(this, 2, effect)
                Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            baron_mordo,
        )
    ]

