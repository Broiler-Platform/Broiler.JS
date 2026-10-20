function T() { this.a = 1; }
var last = null;
for (var i = 0; i < 400; i++) last = new T();
[last.a, Object.keys(last).join(','), JSON.stringify(last), 'a' in last].join('|');