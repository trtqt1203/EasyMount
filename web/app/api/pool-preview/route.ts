import {z} from "zod";
import {previewPool} from "@/lib/easymount";
const schema = z.object({fileGiB:z.number().positive().max(1e9),sources:z.array(z.object({id:z.string().min(1).max(80),name:z.string().min(1).max(100),freeGiB:z.number().min(0).max(1e9)})).min(2).max(100)}).refine(v=>new Set(v.sources.map(s=>s.id)).size===v.sources.length,"Duplicate source ids");
export async function POST(request:Request) {
  if (!request.headers.get("content-type")?.includes("application/json")) return Response.json({error:"Yêu cầu dữ liệu JSON."},{status:415});
  if (Number(request.headers.get("content-length"))>32768) return Response.json({error:"Dữ liệu quá lớn."},{status:413});
  try {
    const text=await request.text();
    if (new TextEncoder().encode(text).byteLength>32768) return Response.json({error:"Dữ liệu quá lớn."},{status:413});
    const data=schema.safeParse(JSON.parse(text));
    if (!data.success) return Response.json({error:"Kiểm tra kích thước file và danh sách nguồn (2–100 nguồn, mã không trùng)."},{status:400});
    return Response.json(previewPool(data.data.sources,data.data.fileGiB),{headers:{"Cache-Control":"no-store"}});
  } catch {return Response.json({error:"JSON không hợp lệ."},{status:400});}
}
