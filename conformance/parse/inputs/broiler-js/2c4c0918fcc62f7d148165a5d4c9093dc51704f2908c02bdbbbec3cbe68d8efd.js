'use strict';
function what() { return typeof this; }
[what(), what(), what(), what()].join('|');