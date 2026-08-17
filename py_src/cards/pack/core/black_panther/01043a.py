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
        # "each [[Black Panther]] upgrade you control", so every one of them is
        # resolved and there is no opting out. `range="All"` fixes the count at
        # the whole candidate set; `(1, "All")` meant "some or all" and let a
        # player resolve one and stop (MARVEL-127/129). The target list is still
        # the *order* of the sequence -- that is the only choice this card
        # grants, and "All" leaves it intact.
        .SetTarget(Upgrade, trait="BLACK PANTHER",
            range="All",
            from_where=["YouControlCards"])
    ]

