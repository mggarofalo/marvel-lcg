from . import *

# "Come Get Me, Bub!"

def GetAbilities() -> Sequence['Ability']:

    def come_get_me_bub(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 3, effect)
        Faces.GiveStatus(effect.targets, "Tough", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            come_get_me_bub
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.DiscardDeckTopUntil("EncounterDeck", Minion, "PutIntoPlayEngagedYou"))
        .SetTarget("YourIdentity", check_fn=lambda effect, face:
            Identity.IsType(face) and (face.CanHeath() or face.CanGainTough())),
    ]

