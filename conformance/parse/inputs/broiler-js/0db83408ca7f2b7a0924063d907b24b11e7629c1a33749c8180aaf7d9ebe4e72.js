(function () {
    var source1 = { default: 42 };
    var source2 = { break: 7 };
    var y1 = ({ default: x } = source1);
    var y2 = ({ bre\u0061k: z } = source2);

    return [
        y1 === source1,
        x,
        y1['default'],
        y2 === source2,
        z,
        y2['break']
    ].join('|');
})()