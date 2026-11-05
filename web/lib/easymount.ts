export const repository = "https://github.com/trtqt1203/EasyMount";
export const providers = [
  {id:"drive",name:"Google Drive",label:"G",color:"#4285F4",authentication:"oauth"},
  {id:"onedrive",name:"OneDrive",label:"O",color:"#0078D4",authentication:"oauth"},
  {id:"dropbox",name:"Dropbox",label:"D",color:"#0061FF",authentication:"oauth"},
];
export type PoolSource = {id:string; name:string; freeGiB:number};
export function previewPool(sources:PoolSource[], fileGiB:number) {
  const selected = sources.find(source => source.freeGiB >= fileGiB);
  return {selectedId:selected?.id ?? null, selectedName:selected?.name ?? null,
    totalFreeGiB:sources.reduce((sum,source)=>sum+source.freeGiB,0),
    explanation:selected ? `File ${fileGiB} GiB được ghi nguyên vẹn vào ${selected.name}.` : "Không nguồn nào đủ chỗ chứa toàn bộ file. File không được chia nhỏ giữa các tài khoản."};
}
