from . import *

# * Strobe

def GetAbilities() -> Sequence['Ability']:

    def strobe_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Stun each character you control",
                lambda targets:
                    Faces.GiveStatus(targets, "Stunned", effect)
            ).SetTarget("YouControlUnit", canbe_stunned=True, range="All"),
            AbilityFactory.ForChoiceAbility(
                "Deal 1 damage to each character you control",
                lambda targets:
                    this.DealDamage(targets, 1, effect)
            ).SetTarget("YouControlUnit", range="All"),
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            strobe_revealed
        ),
    ]

