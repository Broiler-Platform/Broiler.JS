
            var o = {};
            Object.defineProperty(o, 'x', { value: 1, writable: false });
            ++o.x;
        