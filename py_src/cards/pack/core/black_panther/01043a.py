from . import *

# Wakanda Forever!

def GetAbilities() -> Sequence['Ability']:

    def wakanda_forever(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ResolveSpecialAbility(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wakanda_forever
        ).SetPlay()
        .SetTarget(Upgrade, trait="BLACK PANTHER",
            range=(1, "All"),
            from_where=["YouControlCards"])
    ]

