from . import *

def TreatAsMinion(face: 'CardFace', as_card_name: str, engage_player: 'Player', effect: 'Effect', *, process: Callable[['Minion', 'Any', 'Effect'], Any]|None=None) -> bool:
    from game.ability.factory import AbilityFactory
    from game.card.factory import CardFactory
    from game.operate.effects import Effects
    from cards.database import CardsDB
    from game.card.face.card_type import Ally
    from game.card.face.card_type import Hero
    from game.card.face.card_type import Minion
    # from game.card.face.attribute.can_attach import CanAttach

    this = effect.this
    Unused(this)

    paper = CardFactory.FindCardPapers(as_card_name)[0]
    minion = CardFactory.CreateFace(paper, effect.world)

    if (Ally.IsType(face) or Hero.IsType(face)) and Minion.IsType(minion):

        minion.pic_id = face.paper.card_id

        # Hack, for "50171"
        minion.ability.Add(*CardsDB.FindAbilities(paper.card_id, paper.pack, paper.set_name))
        minion.effect.face = minion
        CardFactory.FaceRegisterEffects(minion)

        # minion = face.card.SetAsCard(minion)
        minion = face.card.SwapCardFace(minion, effect)
        minion.printed_attack = face.printed_attack
        minion.printed_scheme = face.printed_thwart

        minion.ResetKeywords() # Hack for 'villainous'

        # if CanAttach.IsType(this):
        #     this.OnAttachTo(minion, False) # Fix "47027"

        if process:
            process(minion, face, effect)

        # Faces.ReadyAll([minion], effect)
        # it does not take consequential damage.
        minion.PutIntoPlay(engage_player, effect)

        # this.OnAttachTo(minion, False)

        effects: List['Effect'] = []

        def action(action_effect: 'Effect', action_message: 'Message2'):
            # minion.PutIntoPlay(player, effect)
            Effects.UnRegister(effects)
            minion.card.SwapCardFace(face, action_effect)
            if face.IsInPlay() and not face.IsDefeated(): # Fix for "34009"
                face.PutIntoPlay(engage_player, effect)

        effects = [
            *minion.effect.RegisterTemp(
                AbilityFactory.AfterCardLeavePlay(
                    AbilityType.Temp2,
                    "This",
                    action,
                    # conditions=[condition]
                ),
                unregister_after_exec=False
            ),
            *this.effect.RegisterTemp(
                AbilityFactory.WhenCardLeavePlay(
                    AbilityType.Temp2,
                    "This",
                    action,
                    conditions=[
                        lambda effect, message:
                            not minion.IsDefeated()
                    ]
                ),
                AbilityFactory.WhenCardTreatAsIfBlank(
                    "This",
                    action,
                ),
                unregister_after_exec=False
            )
        ]
        return True
    return False

class AbilityFactoryTreat:
    @staticmethod
    def TakeControlOfAttachedMinion(as_card_name: str) -> 'Ability':
        from game.operate.faces import Faces
        from game.ability.factory import AbilityFactory
        from game.card.face.card_type import Minion

        def mind_control(effect: 'Effect', message: 'Message.AfterCardAttachTo') -> None:
            initiator = effect.GetInitiator()

            minion = message.to_face.CastTo(Minion)

            Faces.TreatAsAlly(minion, as_card_name, initiator, effect)

        return AbilityFactory.AfterCardGainUpgrade(
            AbilityType.NonKeyword,
            "This",
            Minion,
            mind_control
        )

    @staticmethod
    @deprecated("TreatAttachedCardAsMinion")
    # Attached ally engages its controller
    def TreatAttachedCardAsMinionNew(as_card_name: str,
                                  process: Callable[['Minion', 'Ally', 'Effect'], Any]|None=None) -> 'Ability':
        from game.ability.factory import AbilityFactory
        from game.card.face.card_type import Ally

        def mind_control(effect: 'Effect', message: 'Message.AfterCardAttachTo') -> None:
            player = message.to_face.CastTo(Ally).GetControlByPlayer()
            TreatAsMinion(message.to_face, as_card_name, player, effect, process=process)

        return AbilityFactory.AfterCardGainUpgrade(
            AbilityType.NonKeyword,
            "This",
            Ally,
            mind_control
        )

    @staticmethod
    @deprecated("TreatAttachedCardAsMinion")
    def TreatAttachedHeroAsMinionNew(as_card_name: str,
                                  process: Callable[['Minion', 'Hero', 'Effect'], Any]|None=None) -> 'Ability':
        from game.ability.factory import AbilityFactory
        from game.card.face.card_type import Hero

        def mind_control(effect: 'Effect', message: 'Message.AfterCardAttachTo') -> None:
            player = message.to_face.CastTo(Hero).GetControlByPlayer()
            TreatAsMinion(message.to_face, as_card_name, player, effect, process=process)

        return AbilityFactory.AfterCardGainUpgrade(
            AbilityType.NonKeyword,
            "This",
            Hero,
            mind_control
        )

    # Attached ally engages its controller
    TF_ = TypeVar("TF_", bound='Hero|Ally')
    @staticmethod
    def TreatAttachedCardAsMinion(which_card: Type[TF_],
                                  as_card_name: str,
                                  process: Callable[['Minion', 'TF_', 'Effect'], Any]|None=None) -> 'Ability':
        def treat_attached_ally_as_minion(effect: 'Effect', face: 'AbilityFactoryTreat.TF_', diff: int) -> None:
            from game.card.factory import CardFactory
            # from game.card.face.card_type import Ally
            # from game.card.face.card_type import Hero
            from game.card.face.card_type import Minion
            # from game.player import Player
            from game.operate.faces import Faces
            from cards.database import CardsDB

            this = effect.this
            Unused(this)

            if diff == 1:
                player = face.GetControlByPlayer()
                face = face.CastTo(which_card)

                paper = CardFactory.FindCardPapers(as_card_name)[0]
                minion = CardFactory.CreateFace(paper, effect.world)
                minion.pic_id = face.paper.card_id
                minion.SetName(face.printed_name)
                minion.SetSubtitle(face.printed_subtitle)

                assert Minion.IsType(minion)
                minion.printed_attack = face.printed_attack
                minion.printed_scheme = face.printed_thwart

                # BUG: When blank the attachment twice, it will triggers (diff = 1) x2
                minion = face.card.SwapCardFace(minion, effect, no_detach=True)

                # Fix "01066" "21153"
                if not minion.IsInPlay():
                    return

                # Hack, for "50171"
                minion.ability.Add(*CardsDB.FindAbilities(paper.card_id, paper.pack, paper.set_name))
                minion.effect.face = minion
                CardFactory.FaceRegisterEffects(minion)

                minion.ResetKeywords() # Hack for 'villainous'

                if process:
                    process(minion, face, effect)

                # it does not take consequential damage.
                minion.PutIntoPlay(player, effect)
            else:
                player = face.CastTo(Minion).GetEngagedPlayer()
                face = face.card.SwapBackToPrintedCard(effect)
                if face.IsInPlay() and not face.IsDefeated():
                    face.PutIntoPlay(player, effect)
                else:
                    Faces.DiscardAll([face], effect)

        from game.ability.factory.asset_helper import GiveKeywordToAttachWhenApplyThisInternal

        return GiveKeywordToAttachWhenApplyThisInternal(
            apply=treat_attached_ally_as_minion,
            ignore_flip=False,
        ).SetFuncName("TreatAttachedCardAsMinion")

