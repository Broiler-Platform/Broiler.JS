
            function tag(t) { return t; }
            var results = [];
            for (var i = 0; i < 2; i++) results.push(tag`hello`);
            results[0] === results[1];
        