const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/zzhon/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const folder=path.resolve(__dirname,'../Validation/Flow');
const groups={registration:['OnboardingSplash','Account','ChooseCompanion','PersonalisePet','CreatingPet','MeetPet'],morning:['WakeFeedback','MorningRating','MorningDrink','MorningStretch','MorningTodos','MorningEnd']};
(async()=>{
 for(const [name,pages]of Object.entries(groups)){
  const layers=[];
  for(let i=0;i<pages.length;i++){
   const x=i%3*422+10,y=Math.floor(i/3)*914;
   layers.push({input:Buffer.from(`<svg width="402" height="30"><text x="12" y="22" font-family="Arial" font-size="18" fill="white">${pages[i]}</text></svg>`),left:x,top:y});
   layers.push({input:path.join(folder,'Screens',pages[i]+'.png'),left:x,top:y+32});
  }
  await sharp({create:{width:1266,height:1828,channels:4,background:'#0b2443'}}).composite(layers).png().toFile(path.join(folder,name+'-overview.png'));
 }
 const baseline=JSON.parse(fs.readFileSync(path.join(folder,'original-scenes.json'),'utf8'));
 const crypto=require('crypto');
 const result=Object.entries(baseline).map(([scene,hash])=>({scene,unchanged:crypto.createHash('sha256').update(fs.readFileSync(path.resolve(__dirname,'../Assets/Sleepet/Scenes',scene))).digest('hex')===hash}));
 fs.writeFileSync(path.join(folder,'scene-preservation.json'),JSON.stringify(result,null,2));
 console.log(JSON.stringify(result));
})();
