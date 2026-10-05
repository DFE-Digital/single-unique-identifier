import fs from "node:fs";
import { pathPrefix } from "./path-prefix.js";

fs.writeFileSync(
    "content/assets/_generated-path-prefix.scss",
    `$path-prefix: "${pathPrefix}";\n`
);

console.log(`[sass] path-prefix = ${pathPrefix}`);