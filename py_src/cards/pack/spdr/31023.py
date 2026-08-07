from . import *

# Limitless Stamina

def GetAbilities() -> Sequence['Ability']:

    def limitless_stamina(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            limitless_stamina,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().printed_health >= 14,
            ]
        ).SetPlay().SetLabel()
        .SetTarget("YourHero", canbe_ready=True)
    ]

