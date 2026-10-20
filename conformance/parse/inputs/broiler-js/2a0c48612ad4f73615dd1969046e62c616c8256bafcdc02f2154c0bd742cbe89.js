var log = [];
function base() { log.push('base'); return {}; }
function value() { log.push('value'); return 1; }
base().x = value();
log.join(',');