var o = {}; o.x = 1;
var beforeShapeOnly = o.x;
var beforeDescriptor = Object.getOwnPropertyDescriptor(o, 'x').value;
o.x = 7;
var afterWrite = o.x;
var afterDescriptor = Object.getOwnPropertyDescriptor(o, 'x').value;
beforeShapeOnly + ',' + beforeDescriptor + ',' + afterWrite + ',' + afterDescriptor;