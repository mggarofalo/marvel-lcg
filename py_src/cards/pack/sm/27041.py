from . import *

# * Spider-Woman: Jessica Drew

def GetAbilities() -> Sequence['Ability']:

    def spider_woman(effect: 'Effect') -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        return len(CardFinder(is_confused=True, card_type=Enemy).Checks(Worlds.GetOnFieldEnemies(effect)))


    return [
        AbilityFactory.ReduceCostToPlayThis(
            spider_woman
        ),
    ]

