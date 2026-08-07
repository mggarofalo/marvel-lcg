from . import *

# Magnetic Missile

def GetAbilities() -> Sequence['Ability']:

    def magnetic_missile(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        if Faces.DiscardAll(effect.targets, effect):
            targets2 = effect.targets2
            this.DealDamage(targets2, 5, effect)
            Faces.GiveStatus(targets2, "Stunned", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            magnetic_missile
        ).SetPlay().SetLabel()
        .SetTarget(Enemy, with_attach=CardFinder(name="Wrapped in Metal"))
        .SetTarget2(Enemy),
    ]

