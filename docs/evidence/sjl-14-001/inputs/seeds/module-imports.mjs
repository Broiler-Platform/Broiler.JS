// SJL-14.1, part J seed (Module goal): export declarations, a default class, export lists with renaming and a string
// export name, and top-level await (the Module goal's switch on).
export const answer = 42;
export function twice(v) { return v * 2; }
export default class Point { constructor(x) { this.x = x; } }
const local = 1;
export { local as renamed, local as "string name" };
export let counter = 0;
await Promise.resolve(counter);
