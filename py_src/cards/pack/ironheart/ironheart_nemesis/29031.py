from . import *

# Cyborg Tech

def GetAbilities() -> Sequence['Ability']:

    def cyborg_tech_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        player.DealEncounterCard(this, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            most_traits=True,
            if_cannot_gain_surge=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=3,
            retaliate=1,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            cyborg_tech_boost
        ),
    ]

