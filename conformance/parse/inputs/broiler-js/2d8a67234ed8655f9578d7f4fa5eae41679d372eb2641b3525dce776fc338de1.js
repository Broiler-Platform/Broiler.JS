var fns = [];
for (let i = 0; i < 5; i++) fns.push(function () { return i * 10; });
fns.map(function (f) { return f(); }).join(',');