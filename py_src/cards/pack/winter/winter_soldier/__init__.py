from cards.pack import *

def YouExhaustedCyberneticArmToPayForThisEvent(effect: 'Effect') -> bool:
    for effect in effect.context.paid_this_res_effects:
        if effect.this.IsName("Cybernetic Arm"):
            if effect.ability.flags.is_resource:
                return True
            else:
                return False
    return False

