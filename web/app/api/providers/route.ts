import {providers} from "@/lib/easymount";
export function GET() { return Response.json({providers,accountLimit:null},{headers:{"Cache-Control":"public, max-age=3600"}}); }
