var fs = [];
for (let i = 0; i < 3; i++) { fs.push(function () { return i; }); }
fs[0]() + fs[1]() + fs[2]();