(function () {
    function Foo() {}
    Foo.prototype = Function.prototype;
    var f = new Foo();

    try {
        f.bind();
        return 'no-throw';
    } catch (e) {
        return e.constructor.name;
    }
})()