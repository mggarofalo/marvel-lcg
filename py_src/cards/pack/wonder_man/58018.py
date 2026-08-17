from . import *

# Everywhere All at Once

def GetAbilities() -> Sequence['Ability']:

    def everywhere_all_at_once(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        # `.val`, not `GetResourceIconTypes()`. X on this card is the cost that
        # was paid; the helper counts how many *kinds* of resource were spent,
        # which is Jubilee's mechanic and not this one. Under it, two [[mental]]
        # resources bought one scheme and paying five of a colour still bought
        # one -- and "overpaid" meant "spent two colours". MARVEL-137, only
        # observable once MARVEL-135 let this card be paid for at all.
        paid = effect.GetPaidResources().val
        if paid > len(effect.targets):
            value = 3
        else:
            value = 2
        targets = effect.targets[0:paid]
        this.RemoveThreatFromSchemes(targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            everywhere_all_at_once
        ).SetPlay(only_if_your_identity_has_trait="AERIAL").SetLabel('thwart')
        .SetTarget(Scheme2, range=(1, "All"))
    ]

