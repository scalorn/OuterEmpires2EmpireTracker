const fs=require('fs'),p=process.argv[2],d=[];
process.stdin.on('data',c=>d.push(c));
process.stdin.on('end',()=>{fs.appendFileSync(p,d.join(''));console.log('appended')});
