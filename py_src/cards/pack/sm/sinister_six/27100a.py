from . import *
from cards.pack.sm.campaign import *

# Sinister Synchronization

def GetAbilities() -> Sequence['Ability']:

    def sinister_synchronization_begin(effect: 'Effect', message: 'Message.WhenGameBeginSetup') -> None:
        message.skip_put_villain_into_play = True
        pass

    def sinister_synchronization(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        villains: List[Villain] = []
        # for villain in Worlds.GetVillains(effect):
        #     villain.SetAside(effect)

        for villain in Worlds.GetSetAsideAreaCards(effect):
            if Villain.IsType(villain):
                villains.append(villain)

        value = Worlds.GetPlayerNum(effect) + 1
        for _ in range(value):
            villain = Rand.RandomChoice(villains, effect)
            villains.remove(villain)
            assert villain.card.area.flags.is_set_aside
            villain.PutIntoPlay(first_player, effect)

        PlaceActiveCounterOnLowestActivationOrder(Worlds.GetVillains(effect), effect)

        SetupCards.PutIntoPlay(
            effect,
            name="Light at the End",
            card_type=SchemeSide2
        )
        # if face:
        #     # The easiest solution would be to errata Sinister Synchronization 1A to “Reveal the Light at the End side scheme, Trap! side faceup. – Boggs (Facebook)
        #     face.PutIntoPlay(first_player, effect)
        #     if not face.HasTrait("TRAP!"):
        #         face.card.Flip(effect)

    def get_villain(effect: 'Effect', message: 'Message.GettingVillain') -> 'Villain|None':
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        for villain in Worlds.GetVillains(effect):
            if message.game_area:
                if villain.card.game_area != message.game_area:
                    continue
            if villain.IsActive():
                return villain
        return None

    def campaign(effect: 'Effect', message: 'Message.WhenCampaignSetup'):
        this = effect.this
        value = CampaignLog.GetInt("Waking Nightmare: Victory for Scenario #3 - Mysterio", effect)
        face = Worlds.FindCardOnField(effect, name="Light at the End", card_type=SchemeSide2)
        if face:
            this.PlaceThreatOnSchemes([face], value, effect)

    return [
        *CampaignSetup(4, campaign=campaign),
        AbilityFactory.WhenGameBeginSetup(
            sinister_synchronization_begin
        ),
        AbilityFactory.WhenCardSetup(
            "This",
            sinister_synchronization
        ),
        AbilityFactory.GetVillain(
            get_villain
        ).NoOutOfPlayLimit(),
        # After the villain with the active counter is defeated, move the active counter to the next villain in the activation order. If no other villains are in play, set the active counter aside.
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Scenario,
            Villain,
            lambda effect, message:
                MoveTheActiveCounterToTheNextVillain(effect),
            conditions=[
                lambda effect, message:
                    message.trigger.CastTo(Villain).IsActive(),
            ]
        )
    ]

