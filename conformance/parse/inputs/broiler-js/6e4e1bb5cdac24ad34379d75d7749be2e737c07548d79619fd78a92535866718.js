(async function () {
    var x = 0;
    for await ([x] of [[1]]) {
    }
    return x;
})()