from . import *

# Celestial Armor

def GetAbilities() -> Sequence['Ability']:

    def celestial_armor(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            face = player.DiscardDeckTopCard(effect)
            res = FacesCounter.GetPrintedResources([face])

            if res.y or \
                res.r or \
                res.g:
                villain = Worlds.FindVillain(effect)
            else:
                villain = None
            if villain:
                villains = [villain]
            else:
                villains = []

            if res.y or res.g:
                this.HealthUnits(villains, 2, effect)
            if res.b or res.g:
                identity = player.GetIdentity()
                Faces.GiveStatus([identity], "Confused", effect)
                Faces.DiscardAll([this], effect)
            if res.r or res.g:
                Faces.GiveStatus(villains, "Tough", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.WhenUnitSchemeAgainst(
            AbilityType.ForcedInterrupt,
            Villain,
            "You",
            celestial_armor
        ),
    ]

