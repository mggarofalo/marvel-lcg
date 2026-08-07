from cards.pack import *

def PsiKnifeKatana(res: 'Resources') -> List['Ability']:

    # def psi_knife(effect: 'Effect', message: 'Send.AfterCardResolveAbility|Send.WhenPlayerInTurn') -> None:
    #     this = effect.this.CastTo(Upgrade)
    #     Unused(this)

    #     Faces.FlipAllTo(effect.targets, None, effect)

    def psi_knife_flip(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        player = message.GetToPlayer()
        player.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.FlipAllTo(effect.targets, None, effect)
            ).SetTarget([effect.this])
        )

    def psi_knife(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        Faces.FlipAllTo(effect.targets, None, effect)

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            res,
            ex_operation=psi_knife_flip
        ).SetCostFunc(CostFunc.Exhaust("This")),
        # AbilityFactory.AfterCardResolveAbility(
        #     AbilityType.HeroResponse,
        #     ["HeroResource"],
        #     "ThisFace",
        #     psi_knife
        # ).SetTarget("This"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            psi_knife
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This")
    ]

def GetYouControlPsiKnife(player: 'Player') -> int:
    return player.GetIdentity().GetInventoryDeck().FindCardSize(CardFinder(name="Psi-Knife"))

def GetYouControlPsiKatana(player: 'Player') -> int:
    return player.GetIdentity().GetInventoryDeck().FindCardSize(CardFinder(name="Psi-Katana"))

