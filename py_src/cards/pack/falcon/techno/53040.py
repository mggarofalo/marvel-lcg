from . import *

# Tech-Pac

def GetAbilities() -> Sequence['Ability']:

    def tech_pac(effect: 'Effect', targets: Sequence['CardFace']) -> bool:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        faces = Filter.ByType(targets, HasThwart)
        value = sum(x.thwart for x in faces)
        return value > this.GetBindFace().CastTo(HasScheme).scheme

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Fixer"),
            if_cannot_attach_to=Villain
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Enemy,
            get_new_value=lambda effect, attach, ui:
                len(CardFinder2("TECH").Checks([x for x in attach.GetAttachedAttachments() if x != effect.this], effect)),
            attack=1,
            ex_change_on_event=OnEvent.AttachedMove(Attachment)
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust(
            Select.From("YouControlUnit", range=(1, "All"), check_again_fn=tech_pac)
        ))
    ]

