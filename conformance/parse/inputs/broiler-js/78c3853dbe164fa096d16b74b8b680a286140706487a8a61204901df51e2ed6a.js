// DeltaBlue's exact idiom, three levels deep. One store site, many functions.
Object.defineProperty(Object.prototype, 'inheritsFrom', {
    value: function (shuper) {
        function Inheriter() { }
        Inheriter.prototype = shuper.prototype;
        this.prototype = new Inheriter();
        this.superConstructor = shuper;
    }
});

function Constraint(s) { this.strength = s; }
Constraint.prototype.addConstraint = function () { this.added = true; };
function BinaryConstraint() { this.addConstraint(); }
BinaryConstraint.inheritsFrom(Constraint);
function EqualityConstraint() { EqualityConstraint.superConstructor.call(this); }
EqualityConstraint.inheritsFrom(BinaryConstraint);

var e = new EqualityConstraint();
[
    Object.getPrototypeOf(BinaryConstraint.prototype) === Constraint.prototype,
    Object.getPrototypeOf(EqualityConstraint.prototype) === BinaryConstraint.prototype,
    typeof EqualityConstraint.prototype.addConstraint,
    typeof e.addConstraint,
    e.added === true
].join('|');