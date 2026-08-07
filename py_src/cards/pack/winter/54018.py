from . import *

# * Bambino

def GetAbilities() -> Sequence['Ability']:

    def bambino(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.DealAdditionalDamage(3, effect)
        message.GainOverKill(effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("S.H.I.E.L.D", Unit2),
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui: Identity.IsType(effect.this.CastTo(Upgrade).bind_face),
            restricted=1,
            change_on_event=OnEvent.AttachedMove("This"),
        ),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            "AttachedCharacter",
            bambino,
            is_basic_attack=True
        ).SetCostFunc(CostFunc.Counter("This", 1, 'ammo')),
    ]

