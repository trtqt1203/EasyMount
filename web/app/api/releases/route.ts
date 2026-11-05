import manifest from "@/lib/release-manifest.json";
import {z} from "zod";
const schema=z.discriminatedUnion("status",[
  z.object({status:z.literal("unpublished"),version:z.null(),downloadUrl:z.null(),releasesUrl:z.literal("https://github.com/trtqt1203/EasyMount/releases"),checkedAt:z.string(),message:z.string()}),
  z.object({status:z.literal("published"),version:z.string().regex(/^\d+\.\d+\.\d+$/),downloadUrl:z.string().url().refine(value=>{const url=new URL(value);return url.origin==="https://github.com"&&url.pathname.startsWith("/trtqt1203/EasyMount/releases/download/")&&url.pathname.endsWith("-Setup.exe")}),releaseUrl:z.string().url(),publishedAt:z.string(),size:z.number().positive(),platform:z.literal("windows-x64"),signed:z.boolean(),checkedAt:z.string()})
]);
export function GET() {
  const data=schema.safeParse(manifest);
  if(!data.success)return Response.json({error:"Thông tin bản phát hành chưa hợp lệ."},{status:503});
  return Response.json({...data.data,metadataSource:"release-manifest"},{headers:{"Cache-Control":"public, max-age=300"}});
}
