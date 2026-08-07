from . import *

# Up, Up, and Away

def GetAbilities() -> Sequence['Ability']:

    def up_up_and_away(effect: 'Effect', message: 'Message.AfterEnemyGivenBoostCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Worlds.GetEncounterDeck(effect).GetTop()
        faces = ([face] if face else []) + [message.boost_card]
        Faces.LookAt(faces, initiator, effect)
        initiator.SwapTheseCards(faces, effect)

        # HACK:
        if face not in message.GetAttacker().components.boostable.deck.GetAll():
            face = message.boost_card

        value = FacesCounter.CountTotalBoostStarAndBoost([face])
        initiator.DrawUp(value, effect)

    return [
        AbilityFactory.AfterEnemyGivenBoostCard(
            AbilityType.HeroResponse,
            "AttackingEnemy",
            up_up_and_away
        ).SetPlay().SetLabel('defense')
    ]

