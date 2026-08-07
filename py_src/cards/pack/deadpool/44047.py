from . import *

# Get in Front of Me!

def GetAbilities() -> Sequence['Ability']:

    def get_in_front_of_me(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        message.CancelWhenRevealedEffect(effect)
        villain = Worlds.FindVillain(effect)
        if villain:
            def action(atk_message: 'Message.WhenUnitWouldAttack'):
                def draw_up(def_effect: 'Effect', def_message: 'Message.AfterUnitDefendEnd'):
                    initiator.DrawUp(1, effect)

                def is_ally_or_another_hero(def_effect: 'Effect', def_message: 'Message.AfterUnitDefendEnd'):
                    # if def_message.defender == None:
                    #     return False
                    if def_message.defender == effect.GetInitiator().GetHero():
                        return False
                    return CardFinder(card_type=Ally|Hero).Check(def_message.defender)

                this.effect.RegisterTemp(
                    AbilityFactory.AfterUnitDefendAgainstAttack(
                        AbilityType.Temp0,
                        None,
                        draw_up,
                        against_which_attack=atk_message,
                        conditions=[is_ally_or_another_hero]
                    ),
                    unregister_after_exec=True
                )

            villain.DoAttackYou(initiator, effect, operation=action)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            Treachery,
            "WhenRevealed",
            get_in_front_of_me,
            from_encounter=True,
        ).SetPlay().SetLabel(),
    ]

