// Read-only extraction of the browser's keyframes; no edits to the frontend.
const fs=require('fs');
const css=fs.readFileSync('C:/Users/zzhon/Desktop/info90009/Sleepet-Unity/Sleepet/styles.css','utf8');
const tracks=[];
for(const match of css.matchAll(/@keyframes (splash-[\w-]+)\s*\{((?:[^{}]|\{[^{}]*\})*)\}/g)){
 const frames=[];
 for(const block of match[2].matchAll(/([\d.% ,]+)\{([^}]*)\}/g)){
  const b=block[2];let kind='',x=0,y=0,exit=false;
  const prop=b.match(/(?:^|;)\s*(opacity|translate|scale|height):\s*([^;]+)/);if(!prop)continue;
  kind=prop[1];const nums=prop[2].replace('var(--exit-x)','0').match(/-?\d*\.?\d+/g).map(Number);x=nums[0];y=nums[1]??x;exit=prop[2].includes('var(');
  const ease=b.match(/cubic-bezier\(([^)]+)\)/);const e=ease?ease[1].split(',').map(Number):[0,0,1,1];
  for(const time of block[1].matchAll(/([\d.]+)%/g))frames.push({at:Number(time[1])/100,x,y,exit,x1:e[0],y1:e[1],x2:e[2],y2:e[3]});
  if(!tracks.some(t=>t.name===match[1]))tracks.push({name:match[1],kind,frames});
 }
}
fs.writeFileSync('Assets/Sleepet/Art/Flow/web-motion.json',JSON.stringify({tracks},null,2));
console.log('Imported '+tracks.length+' exact CSS splash tracks.');
