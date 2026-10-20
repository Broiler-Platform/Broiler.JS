String((function(){ try { var config = { server: { tls: { enabled: true } } };
for (var i = 0; i < 200; i++) { config.server.tls.enabled; }
config.server = {};
return config.server.tls.enabled; } catch (e) { return e.message; } return 'no throw'; })())