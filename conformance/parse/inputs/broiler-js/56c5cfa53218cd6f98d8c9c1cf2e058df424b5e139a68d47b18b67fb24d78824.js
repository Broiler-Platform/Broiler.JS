var captured;
{
    var outer = 'before';
    { var inner = 'nested'; }
    captured = function () { return outer + '/' + inner; };
    outer = 'after';
}
captured();