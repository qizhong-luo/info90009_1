// Render the original vector paths, never upscale the old low-resolution PNGs.
const fs = require('fs');
const path = require('path');
const sharp = require('C:/Users/zzhon/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const names = ['s', 'l', 'e-1', 'e-2', 'p', 'e-3', 't'];

async function rasterizeSplashLetter(svg, target) {
  const root = svg.match(/<svg\b[^>]*>/)[0];
  const width = Number(root.match(/\bwidth="([\d.]+)"/)[1]);
  const height = Number(root.match(/\bheight="([\d.]+)"/)[1]);
  // 512 pixels on the longest edge covers the animated title even at 4K.
  // Preserve the source viewport and transparent padding so layout stays intact.
  await sharp(Buffer.from(svg), { density: 72 * 512 / Math.max(width, height) })
    .png().toFile(target);
}

module.exports = { rasterizeSplashLetter };
if (require.main === module) {
  (async () => {
    for (const name of names) {
      const file = `letter-${name}`;
      const svg = fs.readFileSync(path.join(__dirname, 'SourceArt/Splash', file + '.svg'), 'utf8');
      const target = path.join(__dirname, '../Assets/Sleepet/Art/Flow', file + '.png');
      await rasterizeSplashLetter(svg, target);
      const metadata = await sharp(target).metadata();
      if (Math.max(metadata.width, metadata.height) !== 512) throw new Error('Unexpected size: ' + file);
      console.log(`${file}: ${metadata.width}x${metadata.height}`);
    }
  })().catch(error => { console.error(error); process.exitCode = 1; });
}
