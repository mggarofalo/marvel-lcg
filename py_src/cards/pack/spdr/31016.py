from . import *

# Repurpose

def GetAbilities() -> Sequence['Ability']:

    def repurpose(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReadyAll(effect.targets, effect)

        text = initiator.AskChooseOneText(["THW", "ATK", "DEF"])

        faces = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        value = FacesCounter.GetPrintedCost(faces)

        hero = initiator.GetHero()
        if text == 'THW':
            hero.GainUntilRoundEnd(effect, thwart=value)
        if text == 'ATK':
            hero.GainUntilRoundEnd(effect, attack=value)
        if text == 'DEF':
            hero.GainUntilRoundEnd(effect, defense=value)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            repurpose
        ).SetPlay().SetLabel()
        .SetTarget("YourHero")
        .SetCostFunc(CostFunc.Discard(
            card_type=Upgrade,
            trait="TECH",
            from_where=["YouControlCards"])),
    ]

