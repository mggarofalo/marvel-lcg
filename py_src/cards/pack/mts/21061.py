from . import *

# Shield Spell

def GetAbilities() -> Sequence['Ability']:

    def shield_spell(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.PreventDamage("All", effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            shield_spell,
            is_from_attack=True,
        ).SetPlay(only_if_your_identity_has_trait="MYSTIC").SetLabel()
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", lambda effect: effect.GetBindMessage(Message.WhenUnitWouldTakeDamage).will_take_damage)),
    ]

