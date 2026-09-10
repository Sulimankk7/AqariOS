import fs from 'node:fs';
const base='http://127.0.0.1:5236';
const password='QaAudit!20260909';
const evidence=[];
async function request(method,path,body,token){
 const r=await fetch(base+path,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:`Bearer ${token}`}:{})},body:body===undefined?undefined:JSON.stringify(body)});
 const text=await r.text(); let json;try{json=JSON.parse(text)}catch{json=text}
 evidence.push({method,path,status:r.status,code:json?.code,detail:r.ok?undefined:json?.detail});
 fs.writeFileSync(new URL('./evidence/account-provision-http.json',import.meta.url),JSON.stringify(evidence,null,2));
 if(!r.ok)throw new Error(`${method} ${path} ${r.status}: ${JSON.stringify(json)}`);
 return json;
}
const admin=await request('POST','/api/v1/auth/login',{emailOrPhone:'qa-admin@example.invalid',password,rememberMe:false});
const accounts=[];
for(const name of ['A','B']){
 const email=`qa-manager-${name.toLowerCase()}@example.invalid`;
 const registration=await request('POST','/api/v1/auth/register',{fullName:`QA Manager ${name}`,email,password,companyName:`AqariOS QA Test Company ${name}`,companyType:0,countryCode:'JO',preferredLanguage:'ar'});
 await request('POST',`/api/v1/platform/landlord-registrations/${registration.registrationId}/approve`,{},admin.accessToken);
 const login=await request('POST','/api/v1/auth/login',{emailOrPhone:email,password,rememberMe:false});
 accounts.push({email,companyId:login.user.activeCompanyId,userId:login.user.id,registrationId:registration.registrationId,roleCodes:login.user.companyRoles});
}
fs.writeFileSync(new URL('./evidence/qa-accounts.json',import.meta.url),JSON.stringify(accounts,null,2));
console.log('Created and approved two isolated QA companies and managers.');
