import fs from 'node:fs';
const base='http://127.0.0.1:5236', evidence=[];
const output=new URL('./evidence/api-probes.json',import.meta.url);
async function req(method,path,body,token,label=''){
 const r=await fetch(base+path,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:`Bearer ${token}`}:{})},body:body===undefined?undefined:JSON.stringify(body)});
 const text=await r.text();let value;try{value=JSON.parse(text)}catch{value=text}
 evidence.push({label,method,path,status:r.status,body:typeof value==='object'&&value?.accessToken?'[authentication redacted]':value});
 fs.writeFileSync(output,JSON.stringify(evidence,null,2));return {status:r.status,value};
}
for(const p of ['/api/v1/buildings','/api/v1/apartments','/api/v1/leasing/tenants','/api/v1/leasing/contracts','/api/v1/rent-payments','/api/v1/expenses','/api/v1/notifications/me','/api/v1/tenant-portal/me','/api/v1/tenant-portal/payments','/api/v1/maintenance/requests','/api/v1/marketplace/listings/company','/api/v1/documents/buildings/00000000-0000-4000-8000-000000000001'])await req('GET',p,undefined,undefined,'No token');
await req('POST','/api/v1/auth/login',{emailOrPhone:'qa-nonexistent@example.invalid',password:'wrong',rememberMe:false},undefined,'Wrong credentials');
await req('POST','/api/v1/auth/login',{emailOrPhone:'',password:''},undefined,'Empty credentials');
const login=await req('POST','/api/v1/auth/login',{emailOrPhone:'qa-manager-b@example.invalid',password:'QaAudit!20260909',rememberMe:false},undefined,'Manager B authentication');
if(login.status!==200)throw new Error('QA manager login failed');const token=login.value.accessToken;
await req('POST','/api/v1/buildings',{name:'',totalFloors:0,addressCity:'',addressNeighborhood:''},token,'Invalid building validation');
await req('POST','/api/v1/buildings',{name:'QA Isolation Residence B',internalCode:'QA-B-001',totalFloors:2,addressCity:'Amman',addressNeighborhood:'QA Only',buildingType:0,addressGovernorate:0},token,'Company B isolation fixture');
const buildings=await req('GET','/api/v1/buildings',undefined,token,'Verify created building');
const a=await req('POST','/api/v1/auth/login',{emailOrPhone:'qa-manager-a@example.invalid',password:'QaAudit!20260909',rememberMe:false},undefined,'Manager A authentication');
const b=buildings.value.find(x=>x.internalCode==='QA-B-001');
if(b)await req('GET',`/api/v1/buildings/${b.id}`,undefined,a.value.accessToken,'A directly requests B building');
await req('GET','/api/v1/platform/companies',undefined,a.value.accessToken,'Manager attempts system administrator action');
await req('GET','/api/v1/buildings/not-a-uuid',undefined,a.value.accessToken,'Invalid UUID');
await req('GET','/api/v1/buildings/00000000-0000-4000-8000-000000000001',undefined,a.value.accessToken,'Nonexistent UUID');
const file=await req('POST','/api/v1/files/upload-request',{moduleName:'leasing',entityId:'00000000-0000-4000-8000-000000000001',filename:'qa.pdf',mimeType:'application/pdf',sizeBytes:12},a.value.accessToken,'Physical storage upload URL contract');
if(file.value?.uploadUrl)evidence.at(-1).body={...file.value,uploadUrl:file.value.uploadUrl.split('?')[0]+'?[signed parameters redacted]'};
fs.writeFileSync(output,JSON.stringify(evidence,null,2));console.log(evidence.map(x=>`${x.label}: ${x.status}`).join('\n'));
