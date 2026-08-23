from core import *
from game.card.face import *
from game.ability import *
from game.message import *
from game.player import *


"""
Additional forms: Cards with the form keyword grant your identity unique forms, such as Spectrum’s energy form.
These forms are in addition to your identity’s alter-ego and hero forms, and they come with their own conditions for changing into them.
When an identity changes their addition form, it does not count against the once-per-turn limit on flipping from hero to alter-ego (or vice-versa), but it does count as changing form for the purpose of triggering card effects such as Moxie.
"""

class Form:

    MASS_FORM_TYPE = Literal[
        "Intangible", "Dense",
        "Solid", "Phased",
    ]

    SUIT_FORM_TYPE = Literal[
        "Assault", "Stealth",
    ]

    ENERGY_FROM_TYPE =  Literal[
        "Gamma", "Photon", "Pulsar"
    ]

    FORM_TYPE = MASS_FORM_TYPE | SUIT_FORM_TYPE | ENERGY_FROM_TYPE

    ENERGY_FROM_TYPE_LIST: List[ENERGY_FROM_TYPE] = Types.LiteralToList(ENERGY_FROM_TYPE)
    FORM_TYPE_LIST = [value for arg in get_args(FORM_TYPE) for value in get_args(arg)]

    def __init__(self, player: 'Player') -> None:
        self.get_additional_from_function = None
        self.player = player

    def FindSuitForm(self) -> 'Upgrade|None':
        upgrades = self.FindFormUpgradeInternal(face_up=True)
        return upgrades[0] if upgrades else None

    def FindMassForm(self) -> 'Upgrade|None':
        upgrades = self.FindFormUpgradeInternal(face_up=True)
        return upgrades[0] if upgrades else None

    def FindEnergyForm(self) -> 'Upgrade|None':
        upgrades = self.FindFormUpgradeInternal(face_up=True)
        return upgrades[0] if upgrades else None

    def FindAllEnergyForms(self) -> List['Upgrade']:
        return self.FindFormUpgradeInternal()

    def FindFormUpgradeInternal(self, *, face_up: Literal[True]|None=None) -> List['Upgrade']:
        from game.card.face.card_type import Upgrade
        hero = self.player.GetIdentity()
        faces = hero.GetInventoryDeck().FindCards(
            include_removed=True,
            names=Form.FORM_TYPE_LIST,
            is_face_up=face_up,
            card_type=Upgrade,
        )
        return faces

    ################################################################################
    #
    def GetNameInternal(self) -> FORM_TYPE|None:
        upgrades = self.FindFormUpgradeInternal(face_up=True)
        if upgrades:
            for from_name in Form.FORM_TYPE_LIST:
                if upgrades[0].IsName(from_name):
                    return from_name
        return None

    def ChangeFormInternal(self, by_effect: 'Effect', name: "FORM_TYPE|None"=None) -> bool:
        from game.card.card_finder import CardFinder
        from game.operate.faces import Faces
        if name != None and self.IsFormInternal(name):
            return False

        identity = self.player.GetIdentity()
        upgrades = self.FindFormUpgradeInternal(face_up=True)

        all_upgrades = self.FindFormUpgradeInternal()

        rest_upgrades = [x for x in all_upgrades if x not in upgrades]

        if not rest_upgrades:
            if not upgrades:
                # No form upgrade in play at all, so there is no form to change
                # into and nothing to flip. `upgrades[0]` raised IndexError --
                # Vision's Density Manipulation reaches this once his mass form
                # upgrade has left play (MARVEL-158).
                return False
            upgrade = upgrades[0]
        elif name == None:
            upgrade = self.player.AskChooseFace(rest_upgrades, by_effect)
        else:
            faces = CardFinder(name=name).Checks(rest_upgrades)
            if faces:
                upgrade = faces[0]
            else:
                return False

        if upgrade:
            def action():
                assert upgrade != None
                if upgrade in upgrades:
                    # Suit / Mass
                    upgrade.card.Flip(by_effect)
                    assert name == None or upgrade.card.face.IsName(name)
                else:
                    # Energy
                    no_choose_upgrade = [x for x in all_upgrades if x != upgrade]
                    Faces.FlipAllTo(no_choose_upgrade, False, by_effect)
                    upgrade.FlipTo(by_effect, face_up=True)
                return upgrade.card.face
            return identity.ChangeAdditionalFrom(by_effect, action)
        return False

    def ChangeSuitForm(self, by_effect: 'Effect', name: "SUIT_FORM_TYPE|None"=None) -> bool:
        return self.ChangeFormInternal(by_effect, name)

    def ChangeMassForm(self, by_effect: 'Effect', name: "MASS_FORM_TYPE|None"=None) -> bool:
        return self.ChangeFormInternal(by_effect, name)

    def ChangeEnergyForm(self, by_effect: 'Effect', name:ENERGY_FROM_TYPE|None=None) -> bool:
        return self.ChangeFormInternal(by_effect, name)

    ################################################################################
    #
    def SetChangeFromFunc(self, func: Callable[['Player'], str|None]):
        self.get_additional_from_function = func

    def IsFormInternal(self, form: 'FORM_TYPE') -> bool:
        return self.GetNameInternal() == form

    def IsInMassForm(self, mass_form: 'MASS_FORM_TYPE') -> bool:
        return self.IsFormInternal(mass_form)

    def IsInSuitForm(self, suit_form: 'SUIT_FORM_TYPE') -> bool:
        return self.IsFormInternal(suit_form)

    def IsInEnergyForm(self, energy_form: 'ENERGY_FROM_TYPE') -> bool:
        return self.IsFormInternal(energy_form)

