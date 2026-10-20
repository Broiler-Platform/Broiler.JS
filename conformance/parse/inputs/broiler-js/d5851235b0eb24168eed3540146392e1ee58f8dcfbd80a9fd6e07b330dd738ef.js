class Box { constructor() { this.v = 2; } }
function make() { return new Box().v; }
[make(), make(), make(), make()].join('|');