from . import *

# Black Widow's Gauntlet

def GetAbilities() -> Sequence['Ability']:

    def black_widows_gauntlet(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        def discard_this():
            Faces.DiscardAll([this], effect)

        no_preparation = True

        def set_resolved_preparation():
            nonlocal no_preparation
            no_preparation = False

        def check_no_preparation(effect: 'Effect', message: 'Message2') -> bool:
            nonlocal no_preparation
            return no_preparation

        this.effect.RegisterTemp(
            AbilityFactory.AfterUnitAttackUnitInternal(
                AbilityType.HeroResponse,
                None,
                None,
                lambda effect, message:
                    discard_this(),
                atk_message=message,
                conditions=[
                    check_no_preparation
                ]
            ),
            unregister_after_exec=True,
            until_event_end=message
        )

        this.effect.RegisterTemp(
            AbilityFactory.WhenResolvePreparationAbility2(
                AbilityType.Temp0,
                None,
                lambda effect, message:
                    set_resolved_preparation(),
            ),
            unregister_after_exec=True,
            until_event_end=message
        )

    def black_widows_gauntlet_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindCardOnField(effect, name="Black Widow")
        if villain:
            this.AttachTo2(villain, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Black Widow")
        ),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.DelayAbility,
            "YouControlCharacter",
            CardFinder(name="Black Widow"),
            black_widows_gauntlet
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            retaliate=1,
        ),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            black_widows_gauntlet_preparation
        ),
    ]

