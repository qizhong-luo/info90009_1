const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/zzhon/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const source=fs.readFileSync('C:/Users/zzhon/Desktop/info90009/Sleepet-Unity/Sleepet/index.html','utf8');
const art=path.resolve(__dirname,'../Assets/Sleepet/Art/Flow');
const match=source.match(/<svg class="rating-bubble-effects"[\s\S]*?<\/svg>/);
(async()=>{
 for(let n=1;n<=4;n++){
  let svg=match[0].replace('<svg ','<svg xmlns="http://www.w3.org/2000/svg" width="482" height="482" ');
  svg=svg.replace(/<g\b([^>]*data-min-rating="(\d)"[^>]*)>([\s\S]*?)<\/g>/g,(all,attrs,num,body)=>Number(num)===n?`<g ${attrs}>${body}</g>`:'');
  svg=svg.replace(/class="rating-light-band"/g,'fill="none" stroke="url(#ratingLightBand)" stroke-width="5" stroke-linecap="round"');
  svg=svg.replace(/class="rating-light-point"/g,'fill="url(#ratingLightPoint)"');
  svg=svg.replace(/class="rating-point-core"/g,'fill="#fff8e4"');
  await sharp(Buffer.from(svg)).png().toFile(path.join(art,`rating-effects-${n}.png`));
 }
 console.log('Four rating light overlays extracted from the original SVG.');
})();
