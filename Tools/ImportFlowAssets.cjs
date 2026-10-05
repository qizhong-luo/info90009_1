// Read-only source import. Writes only into this Unity project's Art/Flow directory.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require('C:/Users/zzhon/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const { rasterizeSplashLetter } = require('./RasterizeSplashLetters.cjs');
const source = 'C:/Users/zzhon/Desktop/info90009/Sleepet-Unity/Sleepet/assets';
const project = path.resolve(__dirname, '..');
const out = path.join(project, 'Assets/Sleepet/Art/Flow');
const validation = path.join(project, 'Validation/Flow');
fs.mkdirSync(out, {recursive:true}); fs.mkdirSync(validation,{recursive:true});
const scenes = path.join(project,'Assets/Sleepet/Scenes');
const hashes = Object.fromEntries(fs.readdirSync(scenes).filter(n=>n.endsWith('.unity')).map(n=>[n,crypto.createHash('sha256').update(fs.readFileSync(path.join(scenes,n))).digest('hex')]));
if(!fs.existsSync(path.join(validation,'original-scenes.json'))) fs.writeFileSync(path.join(validation,'original-scenes.json'),JSON.stringify(hashes,null,2));
(async()=>{
 const inputs = [];
 for(const folder of ['flow','onboarding/svg','onboarding/splash'])
 for(const file of fs.readdirSync(path.join(source,folder))) {
  if(!/\.(png|jpg|svg|mp4)$/.test(file))continue;
  const from=path.join(source,folder,file), target=path.join(out,file.replace(/\.svg$/,'.png'));
  if(file.endsWith('.svg')) {
   let svg=fs.readFileSync(from,'utf8').replace(/(width|height)="([\d.]+)"/g,(m,key,v)=>Number(v)<1?`${key}="1"`:m);
   try {
    if (folder === 'onboarding/splash' && file.startsWith('letter-'))
     await rasterizeSplashLetter(svg, target);
    else await sharp(Buffer.from(svg),{density:192}).png().toFile(target);
   }
   catch(e) { throw new Error(file+': '+e.message); }
  }
  else fs.copyFileSync(from,target);
  inputs.push({source:path.join(folder,file),target:path.basename(target)});
 }
 fs.copyFileSync(path.join(source,'onboarding/latest/meet-pet.png'),path.join(out,'meet-pet.png'));
 await sharp(path.join(out,'drink-background.png')).blur(9).png().toFile(path.join(out,'drink-blurred.png'));
 await sharp(path.join(out,'stretch-background.png')).blur(10).png().toFile(path.join(out,'stretch-blurred.png'));
 await sharp(path.join(out,'todos-background.png')).blur(10).png().toFile(path.join(out,'todos-blurred.png'));
 fs.writeFileSync(path.join(validation,'asset-import.json'),JSON.stringify(inputs,null,2));
 console.log('Imported '+inputs.length+' source assets. Original scene hashes recorded.');
})();
