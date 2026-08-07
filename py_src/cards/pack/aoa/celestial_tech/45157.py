from . import *

# Celestial Weapon

def GetAbilities() -> Sequence['Ability']:

    def celestial_weapon(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        face = player.DiscardDeckTopCard(effect)
        res = FacesCounter.GetPrintedResources([face])
        if res.y or res.g:
            identity = player.GetIdentity()
            this.DealDamage([identity], 2, effect)
        if res.b or res.g:
            player.DiscardHandCards((1, 1), effect)
        if res.r or res.g:
            identity = player.GetIdentity()
            Faces.GiveStatus([identity], "Stunned", effect)
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            Villain,
            celestial_weapon
        ),
    ]

