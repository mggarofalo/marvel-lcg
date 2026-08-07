from . import *

# Death-Glow

def GetAbilities() -> Sequence['Ability']:

    def death_glow(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.SetAside([this], effect)
        if message.killer and message.killer.IsName("Valkyrie"):
            Faces.ReadyAll([message.killer], effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Enemy
        ).NoOutOfPlayLimit(),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            death_glow
        ),
        AbilityFactory.WhenCardWouldMoveToArea(
            AbilityType.NonKeyword,
            "This",
            lambda effect, message:
                message.ChangeToArea(effect.this.GetOwnerPlayer().set_aside_deck, effect),
            conditions=[
                lambda effect, message:
                    message.into_area.flags.is_discards,
            ]
        )
    ]

