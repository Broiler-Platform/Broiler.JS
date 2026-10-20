(function () {
    var namedSym = Symbol('test262');
    var anonSym = Symbol();
    var obj = {
        xId: function x() {},
        id: function() {},
        arrow: () => {},
        cls: class {},
        gen: function*() {},
        cover: (0, function() {}),
        method() {},
        *methodGen() {},
        [anonSym]: function() {},
        [namedSym]: function() {}
    };

    return [
        obj.xId.name,
        obj.id.name,
        obj.arrow.name,
        obj.cls.name,
        obj.gen.name,
        obj.cover.name === 'cover',
        obj.method.name,
        obj.methodGen.name,
        obj[anonSym].name,
        obj[namedSym].name
    ].join('|');
})()