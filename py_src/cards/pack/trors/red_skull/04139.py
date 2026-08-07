from . import *

# The Red House

def GetAbilities() -> Sequence['Ability']:

    def the_red_house(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.UseATKInsteadTHW(effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Red Skull")
        ),
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.Interrupt,
            None,
            "This",
            the_red_house,
            is_basic_thwart=True,
            conditions=[
                lambda effect, message:
                    Player.IsType(message.trigger.GetControlBy())
            ]
        )
    ]

