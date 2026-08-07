from . import *

# Making an Entrance

def GetAbilities() -> Sequence['Ability']:

    def making_an_entrance(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainTHWForThisThwart(2, effect)
        initiator = effect.GetInitiator()
        hero = initiator.GetHero()

        def heal_damage(thw_effect: 'Effect', thw_message: 'Message.AfterUnitThwartEnd'):
            this.HealthUnits([hero], 2, effect)

        this.effect.RegisterTemp(
            AbilityFactory.AfterUnitThwartAndRemoveLastThreat(
                AbilityType.Temp0,
                None,
                heal_damage,
                conditions=[
                    lambda thw_effect, thw_message:
                        message in thw_message.would_thw_messages
                ]
            ),
            unregister_after_exec=True,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.HeroInterrupt,
            "YourHero",
            None,
            making_an_entrance,
            is_basic_thwart=True,
        ).SetPlay().SetLabel(),
    ]

