from . import *

# * Lady Spider: May Reilly

def GetAbilities() -> Sequence['Ability']:

    def lady_spider(effect: 'Effect', message: 'Message.AfterUnitThwartScheme') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        value = message.remove_threat
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    def you_control_another_web_warrior(effect: 'Effect') -> bool:
        initiator = effect.GetInitiator()
        return initiator.IsControl(CardFinder(trait="WEB-WARRIOR", check_face_fn=lambda face: face != effect.this))

    return [
        AbilityFactory.AfterUnitThwartScheme(
            AbilityType.Response,
            "This",
            None,
            lady_spider,
            remove_threat=True,
            conditions=[
                lambda effect, message:
                    you_control_another_web_warrior(effect),
            ]
        ).SetTarget(Scheme2,
            check_fn=lambda effect, face:
                face != effect.GetBindMessage(Message.AfterUnitThwartScheme).scheme)
    ]

