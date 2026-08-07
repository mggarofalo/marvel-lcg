from . import *

# * Falcon

def GetAbilities() -> Sequence['Ability']:

    def falcon(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        face = Worlds.DiscardEncounterTopCard(effect)
        this.GetBuff(Buff_53001a).Update(face)


    return [
        *AbilityFactory.PlayWithTopCardOfDeckFaceup(
            lambda effect:
                Worlds.GetEncounterDecks(effect),
            only_during_player_phase=True
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("AERIAL"),
            falcon
        ).SetName("Eagle-Eyed"),
    ]

