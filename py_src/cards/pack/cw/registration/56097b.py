from . import *

# Hunting Rebel Heroes

def GetAbilities() -> Sequence['Ability']:

    def hunting_rebel_heroes(effect: 'Effect', message: 'Message.AfterCardGainTrait') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        this.DealDamage([message.trigger], 2, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Hero,
            trait="UNREGISTERED",
            # TODO: Test
            change_on_event=OnEvent.CardInPlay(Hero)
        ),
        AbilityFactory.AfterCardGainTrait(
            AbilityType.ForcedResponse,
            Hero,
            "UNREGISTERED",
            hunting_rebel_heroes,
            gives_by_revealed=True,
            conditions=[
                lambda effect, message:
                    effect.this != message.by_effect.this
            ]
        ),
    ]

