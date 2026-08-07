from . import *

# Honed Technique

def GetAbilities() -> Sequence['Ability']:

    def honed_technique(effect: 'Effect', message: 'Message.WhenPlayerPlayCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        num = FacesCounter.GetPrintedCost([message.played_face])
        this.effect.RegisterTemp(
            AbilityFactory.WhenFaceWouldDealDamage(
                AbilityType.Temp0,
                None,
                lambda deal_effect, deal_message:
                    deal_message.IncreaseDamage(num, effect),
                by_which_effect=message.played_effect,
                allow_zero_damage=True
            ),
            unregister_after_exec=False,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenPlayerPlayCard(
            AbilityType.Interrupt,
            "You",
            CardFinder(card_class="Aggression", trait="ATTACK", card_type=Event),
            honed_technique,
            using_res="B",
        ),
    ]

