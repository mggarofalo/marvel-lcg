from . import *

# * Spider-Byte: Margo Kess

def GetAbilities() -> Sequence['Ability']:

    def spider_byte(effect: 'Effect') -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        return len(initiator.GetControlCards2(CardFinder2("TECH")))

    return [
        AbilityFactory.ReduceCostToPlayThis(
            spider_byte
        ),
    ]

