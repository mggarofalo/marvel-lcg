from . import *

# * Spider-Man

def GetAbilities() -> Sequence['Ability']:

    def venom_blast(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)


    def spider_camouflage(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)
        Faces.GiveStatus(effect.targets2, "Confused", effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            venom_blast
        ).SetName("Venom Blast")
        .SetTarget(Enemy),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            spider_camouflage
        ).SetName("Spider Camouflage")
        .SetTarget(name="Spider-Man", canbe_tough=True)
        .SetTarget2(Enemy, canbe_confused=True),
    ]

