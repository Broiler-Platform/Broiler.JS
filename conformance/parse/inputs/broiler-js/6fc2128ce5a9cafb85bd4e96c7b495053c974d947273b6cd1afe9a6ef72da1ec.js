var obj = {};
Object.defineProperty(obj, "accProperty", {
    set: function () {
        "use strict";
        test262unresolvable = null;
        return 11;
    }
});
obj.accProperty = "overrideData";