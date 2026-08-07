from . import *

# Face the Past

def GetAbilities() -> Sequence['Ability']:

    def face_the_past(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        Faces.ReadyAll(effect.targets, effect)
        initiator.DrawUp(3, effect)

        identity.effect.RegisterTemp(
            *AbilityFactory.UnitCannotAttackTarget(
                "YourIdentity",
                cannot_attack=Villain,
                # cannot_trigger_attack_ability="You",
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )
        Faces.RemoveAllFromGame([this], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            face_the_past
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.SearchForCard(
            CardFinder(
                card_type=Minion,
                check_effect_fn=lambda effect, face:
                    Minion.IsType(face) and \
                    face.IsNemesis(effect.GetInitiator()),
            ), "Reveal",
            include_encounter_deck=True,
            include_encounter_discard_pile=True,
            include_set_aside=True,
            range="All"),
        )
        .SetTarget("YourHero"),
    ]

