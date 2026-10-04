const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/zzhon/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const art=path.resolve('Assets/Sleepet/Art/Flow');
async function svg(name,body,w=256,h=256){await sharp(Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`)).png().toFile(path.join(art,name+'.png'));}
(async()=>{
 await svg('web-rating-shade','<defs><radialGradient id="g" cx="55%" cy="30%" r="75%"><stop stop-color="#051f46" stop-opacity=".3"/><stop offset="1" stop-color="#03173c" stop-opacity=".68"/></radialGradient></defs><circle cx="128" cy="128" r="128" fill="url(#g)"/>');
 await svg('web-rating-light','<defs><radialGradient id="g" cx="55%" cy="30%" r="70%"><stop stop-color="#fffae4" stop-opacity=".3"/><stop offset=".55" stop-color="#ffdfb6" stop-opacity=".14"/><stop offset=".83" stop-color="#ffdfb6" stop-opacity="0"/></radialGradient></defs><circle cx="128" cy="128" r="128" fill="url(#g)"/>');
 await svg('web-drink-glow','<defs><radialGradient id="g" cx="62%" cy="79%" r="80%"><stop stop-color="#ffc289" stop-opacity=".7"/><stop offset=".48" stop-color="#ffd4b7" stop-opacity=".4"/><stop offset=".82" stop-color="#bdc1dd" stop-opacity=".35"/></radialGradient></defs><circle cx="128" cy="128" r="128" fill="url(#g)"/>');
 await svg('web-end-glow','<defs><radialGradient id="g" cx="58%" cy="26%" r="80%"><stop stop-color="#ffc16d" stop-opacity=".53"/><stop offset=".46" stop-color="#b9c3cf" stop-opacity=".26"/><stop offset=".77" stop-color="#6a94c2" stop-opacity=".25"/><stop offset="1" stop-color="#e1a099" stop-opacity=".25"/></radialGradient></defs><circle cx="128" cy="128" r="128" fill="url(#g)"/>');
 await svg('web-up-arrows','<path d="M7 10L12 5L17 10M7 18L12 13L17 18" stroke="white" fill="none" stroke-width="2"/>',24,23);
 await svg('web-reveal','<defs><radialGradient id="g"><stop offset=".65" stop-color="#f6faff" stop-opacity=".12"/><stop offset=".84" stop-color="#f6faff" stop-opacity=".078"/><stop offset="1" stop-color="#f6faff" stop-opacity="0"/></radialGradient></defs><circle cx="128" cy="128" r="128" fill="url(#g)"/>');
 // Same 238x189 image in the browser's 111x189 clipped glass viewport.
 await sharp(path.join(art,'drink-glass.png')).resize(476,378,{fit:'fill'}).extract({left:116,top:0,width:222,height:378}).png().toFile(path.join(art,'web-glass-crop.png'));
 console.log('Prepared native UI effect layers and double-chevron.');
})();
