async function sloppyAsync() {
  try { beforeAwait2 = 1; out.push('ok'); } catch (e) { out.push(e.name); }
  await Promise.resolve(0);
  try { afterAwait2 = 1; out.push('ok'); } catch (e) { out.push(e.name); }
}
sloppyAsync();