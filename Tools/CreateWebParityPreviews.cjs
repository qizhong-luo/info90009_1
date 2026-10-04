const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/zzhon/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const base=path.resolve('Validation/Flow');
async function sheet(names,folder,out,height){
 const layers=[];
 for(let i=0;i<names.length;i++)layers.push({input:await sharp(path.join(base,folder,names[i]+'.png')).resize(402,height,{fit:'contain',background:'#0c2542'}).png().toBuffer(),left:10+(i%3)*412,top:10+Math.floor(i/3)*(height+10)});
 await sharp({create:{width:1246,height:Math.ceil(names.length/3)*(height+10)+10,channels:4,background:'#0c2542'}}).composite(layers).png().toFile(path.join(base,out));
}
(async()=>{
 await sheet(['OnboardingSplash','Account','ChooseCompanion','PersonalisePet','CreatingPet','MeetPet'],'Screens','registration-overview.png',874);
 await sheet(['WakeFeedback','MorningRating','MorningDrink','MorningStretch','MorningTodos','MorningEnd'],'Screens','morning-overview.png',874);
 await sheet(['Home-402x980','Me-402x980','ChooseCompanion-402x980','MorningDrink-402x980','MorningTodos-402x980','MorningEnd-402x980'],'Responsive','responsive-overview.png',980);
 console.log('Updated registration, morning and responsive Unity previews.');
})();
