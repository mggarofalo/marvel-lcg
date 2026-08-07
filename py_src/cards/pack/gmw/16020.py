from . import *

# Flora and Fauna

def GetAbilities() -> Sequence['Ability']:

    def flora_and_fauna(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(targets: Sequence[CardFace]):
            for target in targets:
                if target.IsName("Groot"):
                    Faces.PlaceCountersOn([target], 2, 'growth', effect, maximum=10)
                    Faces.ReadyAll([target], effect)
                if Upgrade.IsType(target) and target.IsClass("IdentitySpecific"):
                    Faces.PlaceCountersOn([target], 2, 'charge', effect)
                    Faces.ReadyAll([target], effect)

        def select_target(effect: 'Effect'):
            return [x for x in Worlds.GetOnFieldCards(effect) if x.IsName("Groot") or Upgrade.IsType(x) and x.IsClass("IdentitySpecific") and x.paper.set_name == "Rocket Raccoon"]

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                action
            ).SetTarget(select_target),
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            flora_and_fauna
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp")
    ]

