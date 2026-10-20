function f() { return x + 1; }
var result;
try {
    f();
    result = "no-error";
} catch (error) {
    result = error.constructor.name;
}
const x = 1;
result;