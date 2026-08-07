from . import *

# Weight of the World

def GetAbilities() -> Sequence['Ability']:

    def weight_of_the_world(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        Faces.RemoveAllFromGame([this], effect)


    return [
        *AbilityFactory.CardCannotReady(
            CardFinder(name="Supernova Helmet"),
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            weight_of_the_world
        ).SetCostFunc(CostFunc.Exhaust("YourAlterEgo"))
    ]

