from . import *

# Dead or Alive

def GetAbilities() -> Sequence['Ability']:

    def dead_or_alive(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        def action(player: 'Player'):
            faces = player.discard_pile.Get()
            face = player.AskChooseFace(faces, effect)
            if face:
                player.GainCard(face, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            highest_printed_hp=True,
            if_cannot_gain_surge=True,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health="3*",
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            dead_or_alive
        ),
    ]

