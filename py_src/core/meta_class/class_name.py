
class ClassNameMeta(type):
    def __str__(cls):
        return cls.__name__

    def __repr__(cls):
        return cls.__name__

