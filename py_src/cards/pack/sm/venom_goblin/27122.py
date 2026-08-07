from . import *

# Symbiotic Monstrosity

def GetAbilities() -> Sequence['Ability']:

    def symbiotic_monstrosity_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)
    
        message.would_atk_message.SetDealIndirectDamage(effect)

    return [
        AbilityFactory.ThisGainKeywordWhileFaceIsInPlay(
            CardFinder2("SYMBIOTE", Environment),
            health=3,
            change_on_event=OnEvent.Trait(EncounterCard)
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            symbiotic_monstrosity_boost,
            during_attack=True
        )
    ]

