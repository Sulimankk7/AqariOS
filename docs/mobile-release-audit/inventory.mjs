import fs from 'node:fs';
import path from 'node:path';
const root=path.resolve('mobile/lib'),out=path.resolve('docs/mobile-release-audit');
const walk=d=>fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(d,e.name)):[path.join(d,e.name)]);
const schema=JSON.parse(fs.readFileSync(path.join(out,'evidence/openapi.json'),'utf8').replace(/^\uFEFF/,''));
const backend=Object.entries(schema.paths).flatMap(([route,v])=>Object.entries(v).filter(([m])=>['get','post','put','patch','delete'].includes(m)).map(([method,op])=>({method:method.toUpperCase(),route,summary:op.summary||'',responses:Object.keys(op.responses||{}).join(','),security:JSON.stringify(op.security||[])})));
const canonical=s=>s.replace(/\$\{[^}]+\}|\$\w+/g,'{}').replace(/\{[^}]*\}/g,'{}').replace(/\/$/,'');
const endpoints=[],screens=[],calls=[];
for(const file of walk(root).filter(f=>f.endsWith('.dart'))){
 const source=fs.readFileSync(file,'utf8'),relative=path.relative(process.cwd(),file).replaceAll('\\','/');
 for(const m of source.matchAll(/['"](\/api\/v1[^'"\r\n]*)['"]/g)){
   const prior=source.slice(Math.max(0,m.index-230),m.index);
   const candidates=[...prior.matchAll(/\.(getJson|getList|getInt|getPdf|getOptionalJson|postJson|postId|postVoid|putJson|patchJson|patchVoid|delete)\s*\(/g)];
   let method=candidates.at(-1)?.[1]?.replace(/^(get|post|put|patch|delete).*$/,'$1').toUpperCase()||'DYNAMIC';
   if(prior.includes("http.Request('POST'"))method='POST';
   const route=m[1],matches=backend.filter(b=>canonical(b.route)===canonical(route)&&(b.method===method||method==='DYNAMIC'));
   endpoints.push({module:relative.split('/')[3]||'core',file:relative,line:source.slice(0,m.index).split('\n').length,method,route,backendMatches:matches.map(b=>`${b.method} ${b.route}`).join(' | '),runtime:'NOT_TESTED'});
 }
 for(const m of source.matchAll(/class\s+([A-Za-z]\w*(?:Screen|Page|Landing|Shell|Playground))\s+extends\s+(StatefulWidget|StatelessWidget)/g))screens.push({screen:m[1],file:relative,line:source.slice(0,m.index).split('\n').length,module:relative.split('/')[3]||'core',kind:m[2],runtime:'NOT_TESTED'});
 for(const m of source.matchAll(/\.(getJson|getList|getInt|getPdf|getOptionalJson|postJson|postId|postVoid|putJson|patchJson|patchVoid|delete)\s*\(\s*([^\r\n]+)/g))calls.push({file:relative,line:source.slice(0,m.index).split('\n').length,call:m[1],argument:m[2]});
}
const csv=(rows)=>{const keys=Object.keys(rows[0]||{});return [keys.join(','),...rows.map(r=>keys.map(k=>'"'+String(r[k]??'').replaceAll('"','""')+'"').join(','))].join('\n')};
fs.writeFileSync(path.join(out,'mobile-api-inventory.csv'),csv(endpoints));
fs.writeFileSync(path.join(out,'mobile-http-call-sites.csv'),csv(calls));
fs.writeFileSync(path.join(out,'mobile-screen-inventory.csv'),csv(screens));
fs.writeFileSync(path.join(out,'backend-api-inventory.csv'),csv(backend));
fs.writeFileSync(path.join(out,'inventory.json'),JSON.stringify({endpoints,screens,calls,backend},null,2));
console.log(JSON.stringify({backendOperations:backend.length,mobileLiteralReferences:endpoints.length,screenClasses:screens.length,httpCallSites:calls.length,matchedLiteralReferences:endpoints.filter(e=>e.backendMatches).length,unresolved:endpoints.filter(e=>!e.backendMatches).map(e=>({method:e.method,route:e.route,file:e.file,line:e.line}))},null,2));
