from . import *

# * Nick Fury

def GetAbilities() -> Sequence['Ability']:

    def nick_fury(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.PlaceTokensOn(effect.targets, 1, 'threat', effect)

    def break_cover(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeSuitForm(effect, "Assault")


    return [
        AbilityFactory.AfterUnitMakeThwart(
            AbilityType.Response,
            "This",
            nick_fury,
            is_basic_thwart=True
        ).SetName("Gather Intel")
        .SetTarget("YourSuitFormUpgrade"),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "You",
            break_cover
        ).SetName("Break Cover")
        .SetTarget("YourSuitFormUpgrade", non_name="Assault"),
    ]

