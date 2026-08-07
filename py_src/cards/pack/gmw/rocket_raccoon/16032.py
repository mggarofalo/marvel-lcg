from . import *

# Schadenfreude

def GetAbilities() -> Sequence['Ability']:

    def schadenfreude(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        hero = effect.GetInitiator().GetHero()

        def heal_damage(damage_effect: 'Effect', damage_message: 'Message.AfterFaceDealDamage') -> None:
            hero.HealHealth(2, effect)

        heal_effects = this.effect.Registers(
            AbilityFactory.AfterYouDealDamage(
                AbilityType.Temp0,
                None,
                heal_damage,
                conditions=[
                    lambda effect, damage_message:
                        Enemy.IsType(damage_message.target)
                ],
            )
        )

        def action():
            Effects.UnRegister(heal_effects)

        RunAt.TurnEnd(effect, action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            schadenfreude
        ).SetPlay().SetLabel(),
    ]

