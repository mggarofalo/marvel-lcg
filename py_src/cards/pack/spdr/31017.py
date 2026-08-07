from . import *

# Thwip! Thwip!

def GetAbilities() -> Sequence['Ability']:

    def thwip_thwip(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            thwip_thwip,
            conditions=[
                lambda effect, message:
                    CardFinder(canbe_stunned=True).Checks(Worlds.GetOnFieldEnemies(effect)) != [],
            ]
        ).SetPlay().SetLabel()
        .SetTarget(Enemy, canbe_stunned=True,
            range=(1, 2))
        .SetCostFunc(CostFunc.DealDamage(1,
            "YouControlUnit",
            trait="WEB-WARRIOR")),
    ]

